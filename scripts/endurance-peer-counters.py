#!/usr/bin/env python3
"""Decode the independent Linux peer's bounded atomic-counter file after its owned child has stopped."""
import os
import pathlib
import stat
import struct


def decode(data, maximum_uncertain_tail=0):
    """A forced kill may interrupt at most one single-threaded request's counter publication."""
    if len(data) != 64 or type(maximum_uncertain_tail) is not int or not 0 <= maximum_uncertain_tail <= 32:
        raise ValueError('Invalid counter ABI or bounded termination tail')
    received, responses, *rest = struct.unpack('<8Q', data)
    operations, invalid = rest[:5], rest[5]
    if responses > received or not 0 <= received - sum(operations) <= maximum_uncertain_tail:
        raise ValueError('Independent peer counters do not reconcile within the declared cut tail')
    return dict(schemaVersion=1, format='linux-little-endian-atomic-u64-v1',
                received=received, acceptedResponses=responses, byOperation=operations,
                validationFailures=invalid, maximumUncertainPublicationTail=maximum_uncertain_tail)


def read(path, maximum_uncertain_tail=0):
    """Never follow a substituted symlink or read an unbounded counter artifact."""
    path = pathlib.Path(path)
    info = path.lstat()
    if not stat.S_ISREG(info.st_mode) or path.is_symlink() or info.st_size != 64:
        raise ValueError('Counter artifact must be a regular 64-byte file')
    with path.open('rb') as stream:
        opened = os.fstat(stream.fileno())
        if (opened.st_dev, opened.st_ino) != (info.st_dev, info.st_ino):
            raise ValueError('Counter artifact identity changed')
        return decode(stream.read(65), maximum_uncertain_tail)
