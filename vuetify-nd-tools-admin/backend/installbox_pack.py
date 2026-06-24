"""InstallBox pack/publish logic — port of NDToolsResourcesPack GetPackJson / GetPackXml."""

from __future__ import annotations

import copy
import hashlib
import json
import re
import shutil
import zipfile
from datetime import datetime
from pathlib import Path
from typing import Any
from xml.dom import minidom
from xml.etree import ElementTree as ET

DIR_TYPE_NAMES = {0: "MaxRoot", 1: "Application"}

PUBLISH_JSON_NAME = "InstallBox_version_full.json"
PUBLISH_XML_NAME = "InstallBox_version_full.xml"
UPDATE_BOX_NAME = "updateBox.txt"

DEV_FIELDS = ("targetpath", "savepath")


def ensure_zip_name(name: str) -> str:
    name = (name or "").strip()
    if not name:
        return name
    if not name.lower().endswith(".zip"):
        return f"{name}.zip"
    return name


def zip_base_name(name: str) -> str:
    name = ensure_zip_name(name)
    if name.lower().endswith(".zip"):
        return name[:-4]
    return name


MS_DATE_EPOCH = -62135596800000


def to_ms_date(dt: datetime | None) -> str:
    if dt is None or dt <= datetime(1970, 1, 1):
        return f"/Date({MS_DATE_EPOCH})/"
    ms = int(dt.timestamp() * 1000)
    return f"/Date({ms})/"


def parse_ms_date(value: Any) -> datetime:
    if value is None or value == "":
        return datetime(1970, 1, 1)
    if isinstance(value, (int, float)):
        ms = int(value)
        if ms <= MS_DATE_EPOCH:
            return datetime(1970, 1, 1)
        return datetime.fromtimestamp(ms / 1000)
    text = str(value).strip()
    match = re.match(r"/Date\((-?\d+)(?:[+-]\d+)?\)/", text)
    if match:
        ms = int(match.group(1))
        if ms <= MS_DATE_EPOCH:
            return datetime(1970, 1, 1)
        return datetime.fromtimestamp(ms / 1000)
    try:
        return datetime.fromisoformat(text)
    except ValueError:
        return datetime(1970, 1, 1)


def get_last_write_time(dir_path: str | None) -> datetime:
    if not dir_path or not str(dir_path).strip():
        return datetime.now()
    root = Path(str(dir_path).strip())
    if not root.is_dir():
        return datetime.now()
    latest = datetime.min
    for path in root.rglob("*"):
        if path.is_file():
            try:
                mtime = datetime.fromtimestamp(path.stat().st_mtime)
                if mtime > latest:
                    latest = mtime
            except OSError:
                continue
    if latest == datetime.min:
        try:
            latest = datetime.fromtimestamp(root.stat().st_mtime)
        except OSError:
            latest = datetime.now()
    return latest


def compute_sha256_base64(file_path: Path) -> str:
    import base64

    digest = hashlib.sha256()
    with file_path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return base64.b64encode(digest.digest()).decode("ascii")


def _dir_type_name(type_value: Any) -> str:
    try:
        idx = int(type_value)
    except (TypeError, ValueError):
        return "MaxRoot"
    return DIR_TYPE_NAMES.get(idx, "MaxRoot")


def refresh_item_change(item: dict[str, Any]) -> None:
    if not isinstance(item, dict):
        return

    children = item.get("child")
    if item.get("isParent") and isinstance(children, list) and children:
        changed = 0
        for child in children:
            if isinstance(child, dict):
                refresh_item_change(child)
                if child.get("ischange"):
                    changed += 1
        item["ischange"] = changed > 0
        return

    targetpath = item.get("targetpath")
    if targetpath and str(targetpath).strip():
        path = Path(str(targetpath).strip())
        if path.is_dir():
            try:
                current = get_last_write_time(str(path))
                item["LastWriteTime"] = to_ms_date(current)
                last_pack = parse_ms_date(item.get("LastPackTime"))
                item["ischange"] = current > last_pack
            except OSError:
                item["ischange"] = True
        else:
            item["ischange"] = True
    else:
        item["ischange"] = True


def refresh_items_changes(items: list[Any]) -> dict[str, int]:
    changed = 0
    total = 0

    def walk(nodes: list[Any]) -> None:
        nonlocal changed, total
        for node in nodes:
            if not isinstance(node, dict):
                continue
            total += 1
            refresh_item_change(node)
            if node.get("ischange"):
                changed += 1

    walk(items if isinstance(items, list) else [])
    return {"total": total, "changedCount": changed}


def scan_parent_directory(parent_path: str) -> dict[str, Any]:
    path = Path(str(parent_path).strip())
    if not path.is_dir():
        raise ValueError(f"父目录不存在或不可访问: {path}")

    parent_name = path.name
    parent_item: dict[str, Any] = {
        "targetpath": str(path),
        "savepath": None,
        "zipname": parent_name,
        "type": 0,
        "dirpath": "scripts",
        "SeriesMin": 2015,
        "SeriesMax": 2025,
        "helplink": "https://sundaybox.cc/",
        "abouttext": "",
        "LastWriteTime": to_ms_date(get_last_write_time(str(path))),
        "LastPackTime": to_ms_date(datetime.min),
        "ischange": True,
        "sha": None,
        "selected": True,
        "IsEnabled": True,
        "quick": False,
        "isParent": True,
        "version": 1.0,
        "child": [],
    }

    children: list[dict[str, Any]] = []
    for subdir in sorted(path.iterdir()):
        if not subdir.is_dir():
            continue
        child_name = subdir.name
        children.append({
            "targetpath": str(subdir),
            "savepath": str(path),
            "zipname": f"{parent_name}{child_name}.zip",
            "type": 0,
            "dirpath": parent_item["dirpath"],
            "SeriesMin": 2015,
            "SeriesMax": 2025,
            "helplink": parent_item["helplink"],
            "abouttext": "",
            "LastWriteTime": to_ms_date(get_last_write_time(str(subdir))),
            "LastPackTime": to_ms_date(datetime.min),
            "ischange": True,
            "sha": None,
            "selected": True,
            "IsEnabled": True,
            "quick": False,
            "isParent": False,
            "version": 1.0,
            "child": None,
        })

    parent_item["child"] = children
    return {"parent": parent_item, "children": children}


def _resolve_zip_target(pack_dir: Path, zipname: str) -> Path:
    return pack_dir / ensure_zip_name(zipname)


def pack_item(
    item: dict[str, Any],
    pack_dir: Path,
    version_raise: bool = True,
    logs: list[str] | None = None,
) -> None:
    if logs is None:
        logs = []

    if item.get("isParent"):
        return

    zipname = item.get("zipname") or ""
    if not str(zipname).strip():
        logs.append("跳过：缺少 zipname")
        return

    target = _resolve_zip_target(pack_dir, str(zipname))
    savepath = item.get("savepath")
    targetpath = item.get("targetpath")

    if target.exists():
        target.unlink()

    copied = False
    if savepath and str(savepath).strip():
        source_zip = Path(str(savepath).strip()) / ensure_zip_name(str(zipname))
        if source_zip.is_file():
            shutil.copy2(source_zip, target)
            logs.append(f"复制压缩包 {source_zip} -> {target}")
            copied = True

    if not copied:
        if not targetpath or not str(targetpath).strip():
            logs.append(f"跳过 {zipname}：缺少 targetpath")
            return
        src_dir = Path(str(targetpath).strip())
        if not src_dir.is_dir():
            logs.append(f"跳过 {zipname}：targetpath 不是目录 {src_dir}")
            return
        with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as zf:
            for file_path in src_dir.rglob("*"):
                if file_path.is_file():
                    arcname = file_path.relative_to(src_dir).as_posix()
                    zf.write(file_path, arcname)
        logs.append(f"创建压缩包 {src_dir} -> {target}")

    now = datetime.now()
    item["LastPackTime"] = to_ms_date(now)
    item["ischange"] = False

    if version_raise:
        try:
            version = float(item.get("version") or 0)
        except (TypeError, ValueError):
            version = 0.0
        item["version"] = round(version + 0.1, 2)
        item["sha"] = compute_sha256_base64(target)


def _iter_pack_leaves(items: list[Any]) -> list[dict[str, Any]]:
    leaves: list[dict[str, Any]] = []

    def walk(nodes: list[Any]) -> None:
        for node in nodes:
            if not isinstance(node, dict):
                continue
            children = node.get("child")
            if node.get("isParent") and isinstance(children, list) and children:
                for child in children:
                    if isinstance(child, dict) and not child.get("isParent"):
                        leaves.append(child)
            elif not node.get("isParent"):
                leaves.append(node)

    walk(items if isinstance(items, list) else [])
    return leaves


def _get_node_by_index_path(items: list[Any], index_path: list[int]) -> dict[str, Any] | None:
    current: Any = items
    node: dict[str, Any] | None = None
    for idx in index_path:
        if not isinstance(current, list) or idx < 0 or idx >= len(current):
            return None
        node = current[idx]
        if not isinstance(node, dict):
            return None
        child = node.get("child")
        current = child if isinstance(child, list) else []
    return node


def pack_items_by_index_paths(
    items: list[Any],
    index_paths: list[list[int]],
    pack_dir: Path,
    version_raise: bool = True,
) -> list[str]:
    logs: list[str] = []
    for index_path in index_paths:
        node = _get_node_by_index_path(items, index_path)
        if node is None:
            logs.append(f"跳过无效 indexPath: {index_path}")
            continue
        if node.get("isParent") and isinstance(node.get("child"), list):
            for child in node["child"]:
                if isinstance(child, dict):
                    pack_item(child, pack_dir, version_raise, logs)
        else:
            pack_item(node, pack_dir, version_raise, logs)
    return logs


def pack_all_leaves(
    items: list[Any],
    pack_dir: Path,
    version_raise: bool = True,
    only_changed: bool = False,
) -> list[str]:
    logs: list[str] = []
    for leaf in _iter_pack_leaves(items):
        if only_changed and not leaf.get("ischange"):
            continue
        pack_item(leaf, pack_dir, version_raise, logs)
    return logs


def _round_version(value: Any) -> str:
    try:
        num = float(value)
    except (TypeError, ValueError):
        num = 1.0
    return f"{round(num, 2):.2f}".rstrip("0").rstrip(".") if round(num, 2) == int(round(num, 2)) else f"{round(num, 2):.2f}"


def bump_global_version(meta: dict[str, Any], version_raise: bool) -> str:
    raw = meta.get("Version") or meta.get("_version") or "1.0"
    try:
        ver = float(raw)
    except (TypeError, ValueError):
        ver = 1.0
    if version_raise:
        ver = round(ver + 0.01, 2)
    version_str = _round_version(ver)
    meta["Version"] = version_str
    meta["_version"] = version_str
    return version_str


def _strip_publish_item(item: dict[str, Any]) -> dict[str, Any]:
    result = copy.deepcopy(item)
    for key in DEV_FIELDS:
        result.pop(key, None)
    children = result.get("child")
    if isinstance(children, list):
        result["child"] = [_strip_publish_item(c) for c in children if isinstance(c, dict)]
    elif not result.get("isParent"):
        result["child"] = None
    return result


def _build_publish_items(items: list[Any]) -> list[dict[str, Any]]:
    published: list[dict[str, Any]] = []
    for item in items if isinstance(items, list) else []:
        if not isinstance(item, dict):
            continue
        pub = _strip_publish_item(item)
        children = item.get("child")
        if item.get("isParent") and isinstance(children, list) and children:
            pub_children: list[dict[str, Any]] = []
            for child in children:
                if not isinstance(child, dict):
                    continue
                pub_child = _strip_publish_item(child)
                pub_child["dirpath"] = item.get("dirpath")
                pub_child["type"] = item.get("type")
                pub_child["selected"] = item.get("selected")
                pub_child["quick"] = item.get("quick")
                pub_children.append(pub_child)
            pub["child"] = pub_children
        published.append(pub)
    return published


def _build_publish_payload(meta: dict[str, Any], items: list[Any]) -> dict[str, Any]:
    payload = copy.deepcopy(meta)
    payload.pop("item", None)
    payload["item"] = _build_publish_items(items)
    return payload


def _serialize_json(data: dict[str, Any]) -> str:
    return json.dumps(data, ensure_ascii=False, indent=2) + "\n"


def _append_site_element(parent: ET.Element, item: dict[str, Any]) -> None:
    site = ET.SubElement(parent, "site")
    zipname = ensure_zip_name(str(item.get("zipname") or ""))
    site.set("name", zipname)
    site.set("path", str(item.get("dirpath") or ""))
    site.set("InstallType", _dir_type_name(item.get("type")))
    site.set("SeriesMin", str(item.get("SeriesMin") if item.get("SeriesMin") is not None else 2015))
    site.set("SeriesMax", str(item.get("SeriesMax") if item.get("SeriesMax") is not None else 2023))
    site.set("helplink", str(item.get("helplink") or ""))
    site.set("abouttext", str(item.get("abouttext") or ""))
    site.set("SHA", str(item.get("sha") or ""))
    version = item.get("version")
    site.set("Ver", str(version if version is not None else 0))

    children = item.get("child")
    if isinstance(children, list):
        for child in children:
            if isinstance(child, dict):
                _append_site_element(site, child)


def serialize_publish_xml(meta: dict[str, Any], items: list[Any]) -> str:
    root = ET.Element("Root")
    version = str(meta.get("Version") or meta.get("_version") or "")
    ET.SubElement(root, "version").text = version
    ET.SubElement(root, "last").text = str(meta.get("LastPack") or meta.get("_lastPack") or "")

    publish_items = _build_publish_items(items)
    for item in publish_items:
        _append_site_element(root, item)

    rough = ET.tostring(root, encoding="unicode")
    parsed = minidom.parseString(f'<?xml version="1.0" encoding="utf-8"?>{rough}')
    return parsed.toprettyxml(indent="  ", encoding="utf-8").decode("utf-8")


def _write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def publish(
    meta: dict[str, Any],
    items: list[Any],
    pack_dir: Path,
    data_dir: Path,
    version_raise: bool = True,
) -> dict[str, Any]:
    logs: list[str] = []
    working_meta = copy.deepcopy(meta) if isinstance(meta, dict) else {}
    working_items = copy.deepcopy(items) if isinstance(items, list) else []

    version_str = bump_global_version(working_meta, version_raise)

    for leaf in _iter_pack_leaves(working_items):
        pack_item(leaf, pack_dir, version_raise, logs)

    publish_payload = _build_publish_payload(working_meta, working_items)
    json_text = _serialize_json(publish_payload)
    xml_text = serialize_publish_xml(working_meta, working_items)

    pack_json = pack_dir / PUBLISH_JSON_NAME
    pack_xml = pack_dir / PUBLISH_XML_NAME
    pack_update = pack_dir / UPDATE_BOX_NAME
    data_json = data_dir / PUBLISH_JSON_NAME
    data_xml = data_dir / PUBLISH_XML_NAME
    data_update = data_dir / UPDATE_BOX_NAME

    for path, content in (
        (pack_json, json_text),
        (pack_xml, xml_text),
        (pack_update, version_str),
        (data_json, json_text),
        (data_xml, xml_text),
        (data_update, version_str),
    ):
        _write_text(path, content if isinstance(content, str) else str(content))
        logs.append(f"写入 {path}")

    last_pack = str(pack_json)
    working_meta["LastPack"] = last_pack
    working_meta["_lastPack"] = last_pack

    return {
        "logs": logs,
        "version": version_str,
        "files": [
            str(pack_json),
            str(pack_xml),
            str(pack_update),
            str(data_json),
            str(data_xml),
            str(data_update),
        ],
        "updatedMeta": working_meta,
        "updatedItems": working_items,
        "packpath": str(pack_dir),
    }
