"""NDTools admin backend configuration."""

from __future__ import annotations

import os
from dataclasses import dataclass, field
from pathlib import Path

DEFAULT_ALLOWED_FILES = frozenset({
    "NDToolsList.json",
    "InstallBox_version_full.json",
    "InstallBox_version_full.xml",
    "NDToolsListC3S3.json",
    "updateBox.txt",
})

AUTH_HEADER = "X-NDTools-Data-Key"


@dataclass
class NDToolsAdminConfig:
    """Runtime paths and auth settings for NDTools admin routes."""

    data_dir: Path
    pack_dir: Path | None = None
    backup_dir: Path | None = None
    upload_dir: Path | None = None
    ndtooldatakey: str = ""
    allowed_files: frozenset[str] = field(default_factory=lambda: DEFAULT_ALLOWED_FILES)

    def __post_init__(self) -> None:
        self.data_dir = Path(self.data_dir)
        if self.pack_dir is None:
            self.pack_dir = self.data_dir
        else:
            self.pack_dir = Path(self.pack_dir)
        if self.backup_dir is None:
            self.backup_dir = Path(__file__).resolve().parent.parent / "backups"
        else:
            self.backup_dir = Path(self.backup_dir)
        if self.upload_dir is None:
            self.upload_dir = Path(__file__).resolve().parent.parent / "uploads"
        else:
            self.upload_dir = Path(self.upload_dir)

    @classmethod
    def from_env(cls, base_dir: Path | None = None) -> NDToolsAdminConfig:
        root = base_dir or Path(__file__).resolve().parent.parent.parent
        data_dir = Path(os.environ.get("DATA_DIR", root))
        pack_dir = Path(os.environ.get("PACK_DIR", data_dir))
        return cls(
            data_dir=data_dir,
            pack_dir=pack_dir,
            ndtooldatakey=os.environ.get(
                "NDTOOLDATAKEY",
                "AK-dfsadfekwjfknv009fdsae216548412348df",
            ),
        )

    def ensure_dirs(self) -> None:
        self.backup_dir.mkdir(parents=True, exist_ok=True)
        self.upload_dir.mkdir(parents=True, exist_ok=True)
