"""Reference implementation of DSP StringBuilderUtility KMG formatting."""

from __future__ import annotations

NUM = "0123456789"
THIN_SPACE = "\u2009"


def write_kmg(
    value,
    last_index: int = 8,
    blank: bool = False,
    blank_char: str = THIN_SPACE,
    fill_char: str = " ",
    decimal: str = ".",
    kind: str = "count",
) -> str:
    buf = [fill_char] * (last_index + 1)
    n = int(value)
    sign = 0
    if n > 0:
        sign = 1
    elif n < 0:
        sign = -1
        n = -n
    pos = last_index
    if kind == "power":
        buf[pos] = "W"
        pos -= 1
    elif kind == "energy":
        buf[pos] = "J"
        pos -= 1
    threshold = 1000 if kind == "si1000" else 10000
    if n < threshold:
        if blank:
            buf[pos] = blank_char
            pos -= 1
        while n > 0:
            buf[pos] = NUM[n % 10]
            pos -= 1
            if pos < 0:
                return "".join(buf)
            n //= 10
    else:
        if n < 1_000_000:
            suffix, scale = "k", 1000
        elif n < 1_000_000_000:
            suffix, scale = "M", 1_000_000
        elif n < 1_000_000_000_000:
            suffix, scale = "G", 1_000_000_000
        elif n < 1_000_000_000_000_000:
            suffix, scale = "T", 1_000_000_000_000
        elif n < 1_000_000_000_000_000_000:
            suffix, scale = "P", 1_000_000_000_000_000
        else:
            suffix, scale = "E", 1_000_000_000_000_000_000
        buf[pos] = suffix
        pos -= 1
        if blank:
            buf[pos] = blank_char
            pos -= 1
        acc = 1
        while n >= 1000:
            n //= 10
            acc *= 10
        while n > 0:
            buf[pos] = NUM[n % 10]
            pos -= 1
            if pos < 0:
                return "".join(buf)
            n //= 10
            acc *= 10
            if acc == scale:
                buf[pos] = decimal
                pos -= 1
                if pos < 0:
                    return "".join(buf)
    if sign == 0:
        buf[pos] = "0"
        pos -= 1
    elif sign < 0:
        buf[pos] = "-"
        pos -= 1
    return "".join(buf)


def format_kmg(value, kind: str = "count", blank: bool = False, decimal: str = ".", trim: bool = True) -> str:
    raw = write_kmg(value, kind=kind, blank=blank, decimal=decimal)
    return raw.strip() if trim else raw
