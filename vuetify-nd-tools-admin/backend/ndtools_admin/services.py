"""Shared helpers for NDTools admin route handlers."""

from __future__ import annotations

import json
import shutil
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime
from pathlib import Path

from flask import request

from ndtools_admin.config import AUTH_HEADER, NDToolsAdminConfig


def get_config() -> NDToolsAdminConfig:
    from flask import current_app

    ext = current_app.extensions.get("ndtools_admin")
    if ext is None:
        raise RuntimeError("NDTools admin routes are not registered on this app")
    return ext


def check_auth(config: NDToolsAdminConfig | None = None) -> tuple[bool, str]:
    config = config or get_config()
    if not config.ndtooldatakey:
        return False, "服务端未配置 NDTOOLDATAKEY"

    provided = request.headers.get(AUTH_HEADER, "").strip()
    if not provided:
        auth = request.headers.get("Authorization", "").strip()
        if auth.lower().startswith("bearer "):
            provided = auth[7:].strip()
    if not provided:
        return False, "缺少鉴权密钥"
    if provided != config.ndtooldatakey:
        return False, "鉴权密钥无效"
    return True, ""


def resolve_file(fileid: str, config: NDToolsAdminConfig | None = None) -> Path | None:
    config = config or get_config()
    if not fileid or fileid not in config.allowed_files:
        return None
    return config.data_dir / fileid


def backup_file(filepath: Path, config: NDToolsAdminConfig | None = None) -> None:
    config = config or get_config()
    if not filepath.exists():
        return
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    dest = config.backup_dir / f"{filepath.stem}_{stamp}{filepath.suffix}"
    shutil.copy2(filepath, dest)


def write_json_file(filepath: Path, data, config: NDToolsAdminConfig | None = None) -> None:
    backup_file(filepath, config)
    text = json.dumps(data, ensure_ascii=False, indent=2)
    filepath.write_text(text + "\n", encoding="utf-8")


def is_safe_fileid(fileid: str) -> bool:
    if not fileid or fileid in (".", ".."):
        return False
    return "/" not in fileid and "\\" not in fileid and ".." not in fileid


def pack_file_candidates(fileid: str) -> list[str]:
    name = fileid.strip()
    if not name:
        return []
    candidates = [name]
    lower = name.lower()
    if lower.endswith(".zip"):
        candidates.append(name[:-4])
    else:
        candidates.append(f"{name}.zip")
    seen: set[str] = set()
    ordered: list[str] = []
    for item in candidates:
        if item not in seen:
            seen.add(item)
            ordered.append(item)
    return ordered


def resolve_pack_dir(packpath: str | None) -> tuple[Path | None, str | None]:
    if not packpath or not str(packpath).strip():
        return None, "未配置 packpath"
    path = Path(str(packpath).strip())
    try:
        if not path.is_dir():
            return None, f"packpath 不是有效目录: {path}"
    except OSError as exc:
        return None, f"无法访问 packpath: {exc}"
    return path, None


def resolve_pack_file_in_dir(fileid: str, pack_dir: Path) -> Path | None:
    if not is_safe_fileid(fileid):
        return None
    for candidate in pack_file_candidates(fileid):
        path = pack_dir / candidate
        if path.is_file():
            return path
    return None


def check_pack_file_exists(fileid: str, pack_dir: Path) -> dict:
    path = resolve_pack_file_in_dir(fileid, pack_dir)
    if path is not None:
        return {
            "exists": True,
            "source": "packpath",
            "filename": path.name,
            "path": str(path),
        }
    return {
        "exists": False,
        "source": "packpath",
        "filename": None,
        "path": None,
    }


def mimetype_for_fileid(fileid: str) -> str:
    lower = fileid.lower()
    if lower.endswith(".xml"):
        return "application/xml"
    if lower.endswith(".txt"):
        return "text/plain; charset=utf-8"
    return "application/json"
