"""Admin APIs for registering stores and POS terminals."""

from datetime import datetime, timezone
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import select
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.core.db import get_db
from app.models.store_pos import PosTerminal, Store
from app.schemas.store_pos import (
    PosTerminalCreateReq,
    PosTerminalResp,
    StoreCreateReq,
    StoreResp,
)


router = APIRouter(
    prefix="/api/v1/admin",
    tags=["Admin Registration"],
)

DbSession = Annotated[Session, Depends(get_db)]


def require_admin() -> None:
    """Temporary authorization dependency pending PB-02 implementation."""
    # TODO(PB-02): verify the caller's admin role or access token.


@router.post(
    "/stores",
    response_model=StoreResp,
    status_code=status.HTTP_201_CREATED,
    dependencies=[Depends(require_admin)],
)
def create_store(payload: StoreCreateReq, db: DbSession) -> Store:
    """Register a new store."""
    if db.get(Store, payload.store_id) is not None:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Store '{payload.store_id}' already exists",
        )

    store = Store(**payload.model_dump())
    db.add(store)
    try:
        db.commit()
    except IntegrityError as exc:
        db.rollback()
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Store '{payload.store_id}' already exists",
        ) from exc

    db.refresh(store)
    return store


@router.post(
    "/pos-terminals",
    response_model=PosTerminalResp,
    status_code=status.HTTP_201_CREATED,
    dependencies=[Depends(require_admin)],
)
def create_pos_terminal(
    payload: PosTerminalCreateReq,
    db: DbSession,
) -> PosTerminal:
    """Register a new POS terminal for an existing store."""
    if db.get(Store, payload.store_id) is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Store '{payload.store_id}' does not exist",
        )

    existing_pos = db.scalar(
        select(PosTerminal).where(PosTerminal.pos_id == payload.pos_id)
    )
    if existing_pos is not None:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"POS terminal '{payload.pos_id}' already exists",
        )

    pos_terminal = PosTerminal(
        **payload.model_dump(),
        registered_at=datetime.now(timezone.utc),
    )
    db.add(pos_terminal)
    try:
        db.commit()
    except IntegrityError as exc:
        db.rollback()
        if db.get(Store, payload.store_id) is None:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Store '{payload.store_id}' does not exist",
            ) from exc
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"POS terminal '{payload.pos_id}' already exists",
        ) from exc

    db.refresh(pos_terminal)
    return pos_terminal


@router.get("/stores/{store_id}", response_model=StoreResp)
def get_store(store_id: str, db: DbSession) -> Store:
    """Return a store by its identifier."""
    store = db.get(Store, store_id)
    if store is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Store '{store_id}' does not exist",
        )
    return store


@router.get("/pos-terminals/{pos_id}", response_model=PosTerminalResp)
def get_pos_terminal(pos_id: str, db: DbSession) -> PosTerminal:
    """Return a POS terminal by its identifier."""
    pos_terminal = db.get(PosTerminal, pos_id)
    if pos_terminal is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"POS terminal '{pos_id}' does not exist",
        )
    return pos_terminal
