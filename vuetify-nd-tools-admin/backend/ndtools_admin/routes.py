"""Flask Blueprint for NDTools admin API routes."""

from __future__ import annotations

import json
import os
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime

from flask import Blueprint, jsonify, request, send_file
from werkzeug.utils import secure_filename

from installbox_pack import (
    pack_all_leaves,
    pack_items_by_index_paths,
    publish,
    refresh_items_changes,
    scan_parent_directory,
)
from ndtools_admin.services import (
    check_auth,
    check_pack_file_exists,
    get_config,
    is_safe_fileid,
    mimetype_for_fileid,
    resolve_file,
    resolve_pack_dir,
    write_json_file,
)

bp = Blueprint("ndtools_admin", __name__)


@bp.get("/health")
def health():
    config = get_config()
    return jsonify({
        "status": "ok",
        "data_dir": str(config.data_dir),
        "pack_dir": str(config.pack_dir),
    })


@bp.post("/installbox/check-files")
def check_installbox_files():
    payload = request.get_json(silent=True) or {}
    files = payload.get("files", [])
    packpath = payload.get("packpath", "")

    if not isinstance(files, list):
        return jsonify({"error": "files 必须是数组"}), 400

    pack_dir, dir_error = resolve_pack_dir(packpath if isinstance(packpath, str) else "")
    if dir_error:
        return jsonify({"error": dir_error, "packpath": packpath}), 400

    results: dict[str, dict] = {}
    missing: list[str] = []
    unique_names: list[str] = []
    for raw_name in files:
        if not isinstance(raw_name, str):
            continue
        name = raw_name.strip()
        if not name or not is_safe_fileid(name):
            continue
        if name in results:
            continue
        unique_names.append(name)

    max_workers = min(12, max(1, len(unique_names)))
    with ThreadPoolExecutor(max_workers=max_workers) as pool:
        futures = {
            pool.submit(check_pack_file_exists, name, pack_dir): name
            for name in unique_names
        }
        for future in as_completed(futures):
            name = futures[future]
            info = future.result()
            results[name] = info
            if not info["exists"]:
                missing.append(name)

    missing.sort()

    return jsonify({
        "results": results,
        "missing": missing,
        "missingCount": len(missing),
        "checkedCount": len(results),
        "packpath": str(pack_dir),
    })


@bp.post("/auth/verify")
def verify_auth():
    ok, msg = check_auth()
    if not ok:
        return jsonify({"success": False, "error": msg}), 401
    return jsonify({"success": True})


@bp.get("/download")
def download():
    fileid = request.args.get("fileid", "")
    filepath = resolve_file(fileid)
    if filepath is None:
        return jsonify({"error": "无效的文件名", "fileid": fileid}), 400
    if not filepath.is_file():
        return jsonify({"error": "文件不存在", "fileid": fileid}), 404
    return send_file(filepath, mimetype=mimetype_for_fileid(fileid))


@bp.post("/installbox/refresh-changes")
def installbox_refresh_changes():
    ok, msg = check_auth()
    if not ok:
        return jsonify({"error": msg}), 401

    payload = request.get_json(silent=True) or {}
    items = payload.get("items", [])
    if not isinstance(items, list):
        return jsonify({"error": "items 必须是数组"}), 400

    stats = refresh_items_changes(items)
    return jsonify({"items": items, **stats})


@bp.post("/installbox/scan-parent")
def installbox_scan_parent():
    ok, msg = check_auth()
    if not ok:
        return jsonify({"error": msg}), 401

    payload = request.get_json(silent=True) or {}
    parent_path = payload.get("parentPath", "")
    if not isinstance(parent_path, str) or not parent_path.strip():
        return jsonify({"error": "parentPath 不能为空"}), 400

    try:
        result = scan_parent_directory(parent_path.strip())
    except ValueError as exc:
        return jsonify({"error": str(exc)}), 400
    except OSError as exc:
        return jsonify({"error": f"无法访问目录: {exc}"}), 400

    return jsonify(result)


@bp.post("/installbox/pack")
def installbox_pack():
    ok, msg = check_auth()
    if not ok:
        return jsonify({"error": msg}), 401

    payload = request.get_json(silent=True) or {}
    items = payload.get("items", [])
    packpath = payload.get("packpath", "")
    version_raise = bool(payload.get("version_raise", True))
    index_paths = payload.get("indexPaths")
    only_changed = bool(payload.get("onlyChanged", False))

    if not isinstance(items, list):
        return jsonify({"error": "items 必须是数组"}), 400

    pack_dir, dir_error = resolve_pack_dir(packpath if isinstance(packpath, str) else "")
    if dir_error:
        return jsonify({"error": dir_error, "packpath": packpath}), 400

    try:
        if isinstance(index_paths, list) and index_paths:
            paths: list[list[int]] = []
            for raw in index_paths:
                if isinstance(raw, list):
                    paths.append([int(x) for x in raw])
            logs = pack_items_by_index_paths(items, paths, pack_dir, version_raise)
        else:
            logs = pack_all_leaves(items, pack_dir, version_raise, only_changed)
    except (TypeError, ValueError) as exc:
        return jsonify({"error": f"indexPaths 无效: {exc}"}), 400
    except OSError as exc:
        return jsonify({"error": f"打包失败: {exc}"}), 500

    return jsonify({
        "success": True,
        "logs": logs,
        "items": items,
        "packpath": str(pack_dir),
    })


@bp.post("/installbox/publish")
def installbox_publish():
    ok, msg = check_auth()
    if not ok:
        return jsonify({"error": msg}), 401

    config = get_config()
    payload = request.get_json(silent=True) or {}
    meta = payload.get("meta", {})
    items = payload.get("items", [])
    packpath = payload.get("packpath", "")
    version_raise = bool(payload.get("version_raise", True))

    if not isinstance(meta, dict):
        return jsonify({"error": "meta 必须是对象"}), 400
    if not isinstance(items, list):
        return jsonify({"error": "items 必须是数组"}), 400

    pack_dir, dir_error = resolve_pack_dir(packpath if isinstance(packpath, str) else "")
    if dir_error:
        return jsonify({"error": dir_error, "packpath": packpath}), 400

    try:
        result = publish(meta, items, pack_dir, config.data_dir, version_raise)
    except OSError as exc:
        return jsonify({"error": f"发布失败: {exc}"}), 500

    return jsonify({"success": True, **result})


@bp.route("/data", methods=["PUT", "POST"])
def save_data():
    ok, msg = check_auth()
    if not ok:
        return jsonify({"error": msg}), 401

    fileid = request.args.get("fileid", "")
    filepath = resolve_file(fileid)
    if filepath is None:
        return jsonify({"error": "无效的文件名", "fileid": fileid}), 400

    payload = request.get_json(silent=True)
    if payload is None:
        return jsonify({"error": "请求体必须是有效的 JSON"}), 400

    try:
        write_json_file(filepath, payload)
    except (TypeError, ValueError) as exc:
        return jsonify({"error": f"无法写入 JSON: {exc}"}), 400
    except OSError as exc:
        return jsonify({"error": f"写入文件失败: {exc}"}), 500

    return jsonify({
        "success": True,
        "message": f"{fileid} 已保存",
        "fileid": fileid,
    })


@bp.post("/upload")
def upload():
    ok, msg = check_auth()
    if not ok:
        return jsonify({"success": False, "message": msg}), 401

    config = get_config()
    if not request.files:
        return jsonify({"success": False, "message": "未收到文件"}), 400

    saved = []
    for key, file in request.files.items():
        if not file or not file.filename:
            continue
        filename = secure_filename(file.filename)
        stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        dest = config.upload_dir / f"{stamp}_{filename}"
        file.save(dest)
        saved.append(dest.name)

    meta = {
        "name": request.form.get("name", ""),
        "seriesMin": request.form.get("seriesMin", ""),
        "seriesMax": request.form.get("seriesMax", ""),
        "description": request.form.get("description", ""),
        "helpUrl": request.form.get("helpUrl", ""),
        "contact": request.form.get("contact", ""),
    }
    if meta["name"]:
        meta_path = config.upload_dir / f"{datetime.now().strftime('%Y%m%d_%H%M%S')}_meta.json"
        meta_path.write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")

    if not saved:
        return jsonify({"success": False, "message": "未收到有效文件"}), 400

    return jsonify({
        "success": True,
        "message": "文件上传成功，等待审核",
        "files": saved,
    })
