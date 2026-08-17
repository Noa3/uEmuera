/**
 * Synthetic ERE game fixture for uEmuera EraElectron compatibility testing.
 * 
 * This file is part of uEmuera and is NOT derived from any commercial game.
 * License: same as uEmuera project.
 * 
 * Tests: core era.* API surface (P0 set).
 */
const era = require('#/era-electron');

/**
 * Main game entry point.
 * Exercises all P0 era.* APIs in sequence.
 */
async function main() {
  // Verify runtime identity
  if (!era.isEra) {
    throw new Error('era.isEra is not true — runtime not injected');
  }
  if (typeof era.version.sdk !== 'string') {
    throw new Error('era.version.sdk missing');
  }

  // Basic output
  era.print('Hello from synthetic ERE fixture');
  era.println();
  era.drawLine();
  era.print('ERA SDK: ' + era.version.sdk);
  era.println();

  // Button + input
  era.printButton('OK', 1);
  const input = await era.input();

  // printAndWait
  await era.printAndWait('Input received: ' + String(input));

  // Data model
  era.set('flag:0', 1);
  const val = era.get('flag:0');
  era.print('flag:0 = ' + String(val));
  era.println();

  era.add('flag:1', 42);
  const val2 = era.get('flag:1');
  era.print('flag:1 after add(42) = ' + String(val2));
  era.println();

  // Wait
  await era.waitAnyKey();

  // Clear
  await era.clear();

  // Save / load
  const saved = await era.saveData(0, 'test save');
  era.print('saveData(0): ' + String(saved));
  era.println();

  const loaded = await era.loadData(0);
  era.print('loadData(0): ' + String(loaded));
  era.println();

  // Line count
  const lines = era.getLineCount();
  era.print('getLineCount: ' + String(lines));
  era.println();

  // Layout defaults
  era.setAlign('center');
  era.setWidth(20);
  era.setOffset(2);
  era.setColor('#cccccc');
  era.print('Centered text');
  era.println();
  era.setAlign('left');
  era.setWidth(24);
  era.setOffset(0);

  // setToBottom
  era.setToBottom();

  // Notify
  era.notify('Test complete', 'Result', 'success', 3000);

  await era.printAndWait('Synthetic fixture complete.');
}

module.exports = main;
