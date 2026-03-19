"""Pydantic models for table tennis match state."""

from datetime import datetime
from enum import Enum
from typing import Optional

from pydantic import BaseModel, Field


class MatchStatus(str, Enum):
    """Match lifecycle status."""

    PENDING = "pending"
    IN_PROGRESS = "in_progress"
    PAUSED = "paused"
    COMPLETED = "completed"
    CANCELLED = "cancelled"


class Player(BaseModel):
    """Table tennis player model."""

    id: str = Field(..., description="Unique player identifier")
    name: str = Field(..., min_length=1, description="Player display name")
    rating: Optional[int] = Field(None, ge=0, description="Optional ELO/rating")


class Score(BaseModel):
    """Score for a single game within a match."""

    player_one_games: int = Field(0, ge=0, description="Games won by player one")
    player_two_games: int = Field(0, ge=0, description="Games won by player two")
    current_game_player_one: int = Field(0, ge=0, description="Points in current game for player one")
    current_game_player_two: int = Field(0, ge=0, description="Points in current game for player two")


class MatchState(BaseModel):
    """Full state of a table tennis match."""

    match_id: str = Field(..., description="Unique match identifier")
    player_one: Player = Field(..., description="First player")
    player_two: Player = Field(..., description="Second player")
    score: Score = Field(default_factory=Score, description="Current score")
    status: MatchStatus = Field(MatchStatus.PENDING, description="Match status")
    games_to_win: int = Field(3, ge=1, le=7, description="Games required to win the match")
    points_to_win_game: int = Field(11, ge=1, le=21, description="Points to win a single game")
    created_at: datetime = Field(default_factory=datetime.utcnow)
    updated_at: datetime = Field(default_factory=datetime.utcnow)
