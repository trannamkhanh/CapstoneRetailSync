"""SQLAlchemy models for stores and POS terminals."""

from datetime import datetime

from sqlalchemy import Boolean, CheckConstraint, DateTime, ForeignKey, Numeric, String, func
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.core.db import Base


class Store(Base):
    """A retail store registered in the synchronization service."""

    __tablename__ = "store"
    __table_args__ = (
        CheckConstraint(
            "latitude BETWEEN -90 AND 90",
            name="ck_store_latitude_range",
        ),
        CheckConstraint(
            "longitude BETWEEN -180 AND 180",
            name="ck_store_longitude_range",
        ),
        {"schema": "dbo"},
    )

    store_id: Mapped[str] = mapped_column(String(10), primary_key=True)
    name: Mapped[str] = mapped_column(String(100), nullable=False)
    latitude: Mapped[float] = mapped_column(Numeric(9, 6), nullable=False)
    longitude: Mapped[float] = mapped_column(Numeric(9, 6), nullable=False)
    region_id: Mapped[str] = mapped_column(String(10), nullable=False)
    is_active: Mapped[bool] = mapped_column(
        Boolean,
        nullable=False,
        default=True,
        server_default="1",
    )

    pos_terminals: Mapped[list["PosTerminal"]] = relationship(
        back_populates="store",
        lazy="selectin",
    )


class PosTerminal(Base):
    """A point-of-sale terminal belonging to a store."""

    __tablename__ = "pos_terminal"
    __table_args__ = (
        CheckConstraint(
            "status IN ('ACTIVE', 'INACTIVE')",
            name="ck_pos_terminal_status",
        ),
        {"schema": "dbo"},
    )

    pos_id: Mapped[str] = mapped_column(String(20), primary_key=True)
    store_id: Mapped[str] = mapped_column(
        ForeignKey("dbo.store.store_id"),
        nullable=False,
        index=True,
    )
    status: Mapped[str] = mapped_column(String(10), nullable=False, default="ACTIVE")
    registered_at: Mapped[datetime] = mapped_column(
        DateTime,
        nullable=False,
        server_default=func.sysutcdatetime(),
    )

    store: Mapped[Store] = relationship(back_populates="pos_terminals")
