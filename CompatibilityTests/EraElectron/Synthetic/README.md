/**
 * README — synthetic ERE test fixture
 * 
 * This directory contains a minimal, uEmuera-owned ERE game fixture for
 * automated compatibility testing.
 *
 * License: same as uEmuera project.
 * Source: original (not derived from any commercial ERA game).
 *
 * Files:
 *   main.js               — game entry point; exercises P0 era.* API set
 *   era-electron-stub.js  — sync/async stubs for headless Node.js testing
 *
 * Usage (headless test):
 *   node -e "
 *     require.resolve = (m) => m === '#/era-electron' ? './era-electron-stub' : require.resolve(m);
 *     require('./main')()
 *   "
 *
 * The following era.* APIs are exercised by main.js:
 *   isEra, version, print, println, drawLine, printButton, input,
 *   printAndWait, set, get, add, waitAnyKey, clear, saveData, loadData,
 *   getLineCount, setAlign, setWidth, setOffset, setColor, setToBottom, notify
 *
 * Expected output (captured in Expected/ when engine is available):
 *   Line 1: "Hello from synthetic ERE fixture"
 *   Line 2: (blank)
 *   Line 3: (divider)
 *   Line 4: "ERA SDK: 4.7.0"
 *   ...
 */
