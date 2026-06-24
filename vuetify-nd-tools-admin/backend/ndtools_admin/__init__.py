"""NDTools admin Flask routes — register into any Flask application."""

from __future__ import annotations

from flask import Blueprint, Flask

from ndtools_admin.config import NDToolsAdminConfig
from ndtools_admin.routes import bp

__all__ = [
    "NDToolsAdminConfig",
    "bp",
    "register_ndtools_admin",
]


def register_ndtools_admin(
    app: Flask,
    config: NDToolsAdminConfig | None = None,
    *,
    url_prefix: str = "",
) -> Blueprint:
    """Register NDTools admin routes on an existing Flask app.

    Example::

        from flask import Flask
        from ndtools_admin import NDToolsAdminConfig, register_ndtools_admin

        app = Flask(__name__)
        register_ndtools_admin(
            app,
            config=NDToolsAdminConfig(
                data_dir="/data/ndtools",
                ndtooldatakey="your-secret",
            ),
            url_prefix="/api",
        )
    """
    if config is None:
        config = NDToolsAdminConfig.from_env()
    config.ensure_dirs()
    app.extensions["ndtools_admin"] = config
    app.register_blueprint(bp, url_prefix=url_prefix)
    return bp
