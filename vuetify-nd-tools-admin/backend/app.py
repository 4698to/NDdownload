"""Standalone entry point for NDTools admin backend."""

from __future__ import annotations

import os

from flask import Flask
from flask_cors import CORS

from ndtools_admin import NDToolsAdminConfig, register_ndtools_admin


def create_app(config: NDToolsAdminConfig | None = None) -> Flask:
    app = Flask(__name__)
    CORS(app)
    register_ndtools_admin(app, config=config)
    return app


app = create_app()


if __name__ == "__main__":
    port = int(os.environ.get("PORT", 8019))
    app.run(host="0.0.0.0", port=port, debug=True)
