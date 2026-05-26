"""Flask backend for NDTools admin — serve and edit JSON data files."""

from __future__ import annotations

import json
import os
import shutil
from datetime import datetime
from pathlib import Path

from flask import Flask, jsonify, request, send_file
from flask_cors import CORS
from werkzeug.utils import secure_filename

BASE_DIR = Path(__file__).resolve().parent.parent
DATA_DIR = Path(os.environ.get("DATA_DIR", BASE_DIR))
BACKUP_DIR = Path(__file__).resolve().parent / "backups"
UPLOAD_DIR = Path(__file__).resolve().parent / "uploads"

ALLOWED_FILES = frozenset({
    "NDToolsList.json",
    "InstallBox_version_full.json",
    "NDToolsListC3S3.json",
})

app = Flask(__name__)
CORS(app)

BACKUP_DIR.mkdir(parents=True, exist_ok=True)
UPLOAD_DIR.mkdir(parents=True, exist_ok=True)

AUTH_HEADER = "X-NDTools-Data-Key"
NDTOOLDATAKEY = os.environ.get(
    "NDTOOLDATAKEY",
    "AK-dfsadfekwjfknv009fdsae216548412348df",
)


def _check_auth() -> tuple[bool, str]:
    if not NDTOOLDATAKEY:
        return False, "服务端未配置 NDTOOLDATAKEY"
    provided = request.headers.get(AUTH_HEADER, "").strip()
    if not provided:
        auth = request.headers.get("Authorization", "").strip()
        if auth.lower().startswith("bearer "):
            provided = auth[7:].strip()
    if not provided:
        return False, "缺少鉴权密钥"
    if provided != NDTOOLDATAKEY:
        return False, "鉴权密钥无效"
    return True, ""

def _resolve_file(fileid: str) -> Path | None:
    if not fileid or fileid not in ALLOWED_FILES:
        return None
    return DATA_DIR / fileid


def _backup_file(filepath: Path) -> None:
    if not filepath.exists():
        return
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    dest = BACKUP_DIR / f"{filepath.stem}_{stamp}{filepath.suffix}"
    shutil.copy2(filepath, dest)


def _read_json_file(filepath: Path):
    raw = filepath.read_text(encoding="utf-8-sig")
    return json.loads(raw)


def _write_json_file(filepath: Path, data) -> None:
    _backup_file(filepath)
    text = json.dumps(data, ensure_ascii=False, indent=2)
    filepath.write_text(text + "\n", encoding="utf-8")


@app.get("/health")
def health():
    return jsonify({"status": "ok", "data_dir": str(DATA_DIR)})


@app.post("/auth/verify")
def verify_auth():
    ok, msg = _check_auth()
    if not ok:
        return jsonify({"success": False, "error": msg}), 401
    return jsonify({"success": True})


@app.get("/download")
def download():
    fileid = request.args.get("fileid", "")
    filepath = _resolve_file(fileid)
    if filepath is None:
        return jsonify({"error": "无效的文件名", "fileid": fileid}), 400
    if not filepath.is_file():
        return jsonify({"error": "文件不存在", "fileid": fileid}), 404
    return send_file(filepath, mimetype="application/json")


@app.route("/data", methods=["PUT", "POST"])
def save_data():
    ok, msg = _check_auth()
    if not ok:
        return jsonify({"error": msg}), 401

    fileid = request.args.get("fileid", "")
    filepath = _resolve_file(fileid)
    if filepath is None:
        return jsonify({"error": "无效的文件名", "fileid": fileid}), 400

    payload = request.get_json(silent=True)
    if payload is None:
        return jsonify({"error": "请求体必须是有效的 JSON"}), 400

    try:
        _write_json_file(filepath, payload)
    except (TypeError, ValueError) as exc:
        return jsonify({"error": f"无法写入 JSON: {exc}"}), 400
    except OSError as exc:
        return jsonify({"error": f"写入文件失败: {exc}"}), 500

    return jsonify({
        "success": True,
        "message": f"{fileid} 已保存",
        "fileid": fileid,
    })


@app.post("/upload")
def upload():
    ok, msg = _check_auth()
    if not ok:
        return jsonify({"success": False, "message": msg}), 401

    if not request.files:
        return jsonify({"success": False, "message": "未收到文件"}), 400

    saved = []
    for key, file in request.files.items():
        if not file or not file.filename:
            continue
        filename = secure_filename(file.filename)
        stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        dest = UPLOAD_DIR / f"{stamp}_{filename}"
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
        meta_path = UPLOAD_DIR / f"{datetime.now().strftime('%Y%m%d_%H%M%S')}_meta.json"
        meta_path.write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")

    if not saved:
        return jsonify({"success": False, "message": "未收到有效文件"}), 400

    return jsonify({
        "success": True,
        "message": "文件上传成功，等待审核",
        "files": saved,
    })


if __name__ == "__main__":
    port = int(os.environ.get("PORT", 8019))
    app.run(host="0.0.0.0", port=port, debug=True)
