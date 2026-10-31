"""Pydantic request and response schemas for store/POS registration."""

from datetime import datetime
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class StoreCreateReq(BaseModel):
    """Payload used to register a store."""

    store_id: str = Field(min_length=1, max_length=10)
    name: str = Field(min_length=1, max_length=100)
    latitude: float = Field(ge=-90.0, le=90.0)
    longitude: float = Field(ge=-180.0, le=180.0)
    region_id: str = Field(min_length=1, max_length=10)
    is_active: bool = True


class StoreResp(BaseModel):
    """Store information returned by the admin API."""

    model_config = ConfigDict(from_attributes=True)

    store_id: str
    name: str
    latitude: float
    longitude: float
    region_id: str
    is_active: bool


class PosTerminalCreateReq(BaseModel):
    """Payload used to register a POS terminal."""

    pos_id: str = Field(min_length=1, max_length=20)
    store_id: str = Field(min_length=1, max_length=10)
    status: Literal["ACTIVE", "INACTIVE"] = "ACTIVE"


class PosTerminalResp(BaseModel):
    """POS terminal information returned by the admin API."""

    model_config = ConfigDict(from_attributes=True)

    pos_id: str
    store_id: str
    status: str
    registered_at: datetime
