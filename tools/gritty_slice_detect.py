#!/usr/bin/env python3
"""Gutter/edge detection helpers for Gritty UI sheet slicing."""

from __future__ import annotations

from dataclasses import dataclass

import cv2
import numpy as np


@dataclass
class Box:
    x0: int
    y0: int
    x1: int
    y1: int

    @property
    def w(self) -> int:
        return self.x1 - self.x0

    @property
    def h(self) -> int:
        return self.y1 - self.y0


def _sobel_x_peaks(band: np.ndarray, thresh: float = 22.0, min_sep: int = 60) -> list[int]:
    gx = cv2.Sobel(band, cv2.CV_64F, 1, 0, ksize=3)
    edge = np.abs(gx).mean(axis=0)
    sm = np.convolve(edge, np.ones(11, dtype=np.float64) / 11.0, mode="same")
    peaks: list[int] = []
    for i in range(12, len(sm) - 12):
        if sm[i] >= thresh and sm[i] >= sm[i - 1] and sm[i] >= sm[i + 1]:
            if not peaks or i - peaks[-1] > min_sep:
                peaks.append(i)
    return peaks


def column_starts_from_peaks(peaks: list[int], cols: int, min_pitch: int = 400) -> list[int] | None:
    if len(peaks) < cols:
        return None
    starts = [peaks[0]]
    for p in peaks[1:]:
        if p - starts[-1] >= min_pitch:
            starts.append(p)
        if len(starts) == cols:
            return starts
    return starts if len(starts) == cols else None


def detect_row_x_bounds(gray: np.ndarray, y0: int, y1: int, cols: int) -> list[tuple[int, int]] | None:
    band = gray[y0:y1, :]
    peaks = _sobel_x_peaks(band)
    starts = column_starts_from_peaks(peaks, cols)
    if starts is None:
        return None

    widths = [starts[i + 1] - starts[i] for i in range(len(starts) - 1)]
    median_w = int(np.median(widths)) if widths else 520
    bounds: list[tuple[int, int]] = []
    for i, x0 in enumerate(starts):
        if i + 1 < len(starts):
            x1 = starts[i + 1]
        else:
            x1 = min(gray.shape[1], x0 + median_w)
        bounds.append((x0, x1))
    return bounds


def _sobel_y_peaks(band: np.ndarray, thresh: float = 18.0, min_sep: int = 40) -> list[int]:
    gy = cv2.Sobel(band, cv2.CV_64F, 0, 1, ksize=3)
    edge = np.abs(gy).mean(axis=1)
    sm = np.convolve(edge, np.ones(11, dtype=np.float64) / 11.0, mode="same")
    peaks: list[int] = []
    for i in range(12, len(sm) - 12):
        if sm[i] >= thresh and sm[i] >= sm[i - 1] and sm[i] >= sm[i + 1]:
            if not peaks or i - peaks[-1] > min_sep:
                peaks.append(i)
    return peaks


def row_bounds_from_fracs(
    h: int,
    rows: int,
    row_fracs: list[float] | None,
    margin: int = 172,
    gutter_y: int = 34,
) -> list[tuple[int, int]]:
    if row_fracs is None:
        row_fracs = [1.0 / rows] * rows
    inner_h = h - 2 * margin - (rows - 1) * gutter_y
    total = sum(row_fracs)
    out: list[tuple[int, int]] = []
    y = float(margin)
    for frac in row_fracs:
        rh = inner_h * frac / total
        out.append((int(y), int(y + rh)))
        y += rh + gutter_y
    return out


def detect_row_y_bounds(gray: np.ndarray, rows: int, row_fracs: list[float] | None = None) -> list[tuple[int, int]]:
    """Row bands from layout fractions — icon sheets use uneven row heights."""
    return row_bounds_from_fracs(gray.shape[0], rows, row_fracs)


def refine_box(gray: np.ndarray, box: Box, pad: int = 10, shrink: float = 0.08) -> Box:
    """Snap crop to largest foreground blob inside the cell."""
    h, w = gray.shape
    dx = max(2, int(box.w * shrink))
    dy = max(2, int(box.h * shrink))
    x0 = min(w, max(0, box.x0 + dx))
    y0 = min(h, max(0, box.y0 + dy))
    x1 = min(w, max(x0 + 1, box.x1 - dx))
    y1 = min(h, max(y0 + 1, box.y1 - dy))
    inner = gray[y0:y1, x0:x1]
    if inner.size == 0:
        return box

    _, bw = cv2.threshold(inner, 42, 255, cv2.THRESH_BINARY)
    bw = cv2.morphologyEx(bw, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    n, _, stats, _ = cv2.connectedComponentsWithStats(bw)
    if n <= 1:
        return box

    idx = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    ix, iy, iw, ih, _ = stats[idx]
    return Box(
        max(0, x0 + ix - pad),
        max(0, y0 + iy - pad),
        min(w, x0 + ix + iw + pad),
        min(h, y0 + iy + ih + pad),
    )


def build_detected_icon_grid(
    gray: np.ndarray,
    cols: int,
    rows: int,
    row_fracs: list[float] | None,
    margin: int = 172,
    gutter_y: int = 34,
) -> list[Box]:
    h, w = gray.shape
    row_bounds = row_bounds_from_fracs(h, rows, row_fracs, margin, gutter_y)

    best_bounds: list[tuple[int, int]] | None = None
    best_score = -1.0
    for y0, y1 in row_bounds:
        x_bounds = detect_row_x_bounds(gray, y0, y1, cols)
        if not x_bounds:
            continue
        widths = [x1 - x0 for x0, x1 in x_bounds]
        # Prefer rows with full-width, even columns (icon rows beat sparse rows).
        score = sum(widths) - float(np.std(widths)) * 2.0
        if score > best_score:
            best_score = score
            best_bounds = x_bounds

    if best_bounds is None:
        inner_w = w - 2 * margin
        cell_w = (inner_w - (cols - 1) * 28) / cols
        best_bounds = []
        x = float(margin)
        for _ in range(cols):
            best_bounds.append((int(x), int(x + cell_w)))
            x += cell_w + 28

    median_w = int(np.median([x1 - x0 for x0, x1 in best_bounds]))
    last_x0, last_x1 = best_bounds[-1]
    if last_x1 - last_x0 < median_w * 0.85:
        best_bounds[-1] = (last_x0, min(w - margin, last_x0 + median_w))

    boxes: list[Box] = []
    for y0, y1 in row_bounds:
        x_bounds = detect_row_x_bounds(gray, y0, y1, cols) or best_bounds
        if len(x_bounds) != cols:
            x_bounds = best_bounds
        for x0, x1 in x_bounds:
            boxes.append(Box(x0, y0, x1, y1))
    return boxes
