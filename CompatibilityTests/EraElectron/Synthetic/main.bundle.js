(function () {
  'use strict';

  window.game = async function () {
    const era = window.era || window._era;
    if (!era || !era.isEra)
      throw new Error('uEmuera bridge was not injected before main.bundle.js');

    if (!era.version || typeof era.version.sdk !== 'string')
      throw new Error('era.version.sdk missing');

    await era.clear();
    era.setAlign('center');
    era.drawLine({ isSolid: true });
    era.print('uEmuera EraElectron smoke test');
    era.print('SDK: ' + era.version.sdk + ' / Engine: ' + era.version.engine);
    era.drawLine({ isSolid: true });

    era.printButton('Continue', 1);
    era.setAlign('left');
    const input = await era.input();
    if (input !== 1)
      throw new Error('Expected button accelerator 1, got ' + String(input));

    await era.clear();
    era.print('Input bridge: OK');

    era.set('flag:0', 41);
    era.add('flag:0', 1);
    if (era.get('flag:0') !== 42)
      throw new Error('Data bridge failed: flag:0 != 42');
    era.print('Data bridge: OK');

    const saved = await era.saveData(0, 'synthetic smoke save');
    era.print('Save bridge: ' + String(saved));

    await era.delay(20);
    era.notify('Synthetic smoke test reached interactive completion',
      'uEmuera', 'success', 2500);

    era.drawLine({ isSolid: true });
    era.print('PASS: packaged ERE startup, input, data and async bridge are responsive.');
    era.print('Close the EraElectron window to verify return-to-library cleanup.');
    await era.waitAnyKey();
  };
})();
