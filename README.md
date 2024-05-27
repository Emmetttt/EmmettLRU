# Least Recently Used (LRU) Cache Implementation

## Overview

This project contains a custom implementation of a Least Recently Used (LRU) Cache in C#.

The implementation is thread safe, and exhibits `O(1)` write and read times. To ensure thread safety, an aggressive locking strategy is employed on both reads and writes to ensure the correct values are being ejected from the backing linked list. Multi-threaded throughput might be improved by relaxing this requirement.

Benchmarks show, when accessed from a single thread, writes on average take 40ns and reads take on average 9ns.
