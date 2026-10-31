"""Database engine, session factory, and FastAPI dependency."""

from collections.abc import Generator
from pathlib import Path

from pydantic import AliasChoices, Field
from pydantic_settings import BaseSettings, SettingsConfigDict
from sqlalchemy import create_engine
from sqlalchemy.engine import URL
from sqlalchemy.orm import DeclarativeBase, Session, sessionmaker


ENV_FILE = Path(__file__).resolve().parents[2] / ".env"


class DatabaseSettings(BaseSettings):
    """SQL Server connection settings loaded from environment variables."""

    model_config = SettingsConfigDict(
        env_file=ENV_FILE,
        env_file_encoding="utf-8",
        extra="ignore",
    )

    db_host: str = Field(
        default="127.0.0.1",
        validation_alias=AliasChoices("DB_HOST", "MSSQL_SERVER"),
    )
    db_port: int = Field(
        default=14333,
        validation_alias=AliasChoices("DB_PORT", "MSSQL_PORT"),
    )
    db_user: str = Field(
        default="sa",
        validation_alias=AliasChoices("DB_USER", "MSSQL_USER"),
    )
    db_password: str = Field(
        validation_alias=AliasChoices("DB_PASSWORD", "MSSQL_PASSWORD"),
    )
    db_name: str = Field(
        default="RTS_DB",
        validation_alias=AliasChoices("DB_NAME", "MSSQL_DATABASE"),
    )
    db_driver: str = Field(
        default="ODBC Driver 18 for SQL Server",
        validation_alias="DB_DRIVER",
    )
    db_encrypt: str = Field(
        default="yes",
        validation_alias="DB_ENCRYPT",
    )
    db_trust_server_certificate: str = Field(
        default="yes",
        validation_alias="DB_TRUST_SERVER_CERTIFICATE",
    )


class Base(DeclarativeBase):
    """Declarative base shared by SQLAlchemy models."""


settings = DatabaseSettings()

database_url = URL.create(
    drivername="mssql+pyodbc",
    username=settings.db_user,
    password=settings.db_password,
    host=settings.db_host,
    port=settings.db_port,
    database=settings.db_name,
    query={
        "driver": settings.db_driver,
        "Encrypt": settings.db_encrypt,
        "TrustServerCertificate": settings.db_trust_server_certificate,
    },
)

engine = create_engine(database_url, pool_pre_ping=True)

SessionLocal = sessionmaker(
    bind=engine,
    autocommit=False,
    autoflush=False,
    expire_on_commit=False,
)


def get_db() -> Generator[Session, None, None]:
    """Yield a database session and always release it after the request."""
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
