import fable from 'rollup-plugin-fable';
import cleanup from 'rollup-plugin-cleanup';
import filesize from 'rollup-plugin-filesize';
import { resolveBabelOptions } from 'fable-utils';

const getPlugins = () => [
  fable({
    babel: resolveBabelOptions({
      presets: [['env', { targets: { node: 'current' }, modules: false }]],
      plugins: [],
      babelrc: false
    })
  }),
  cleanup(),
  filesize()
];

export default [
  {
    input: 'IML.EventListener/src/IML.EventListener.fsproj',
    external: ['net', 'buffer', 'stream'],
    output: {
      banner: '#!/usr/bin/env node',
      file: './dist/event-listener/event-listener',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.DeviceScannerDaemon/src/IML.DeviceScannerDaemon.fsproj',
    external: ['stream', 'net', 'child_process', 'buffer'],
    output: {
      file: './dist/device-scanner-daemon/device-scanner-daemon',
      format: 'cjs'
    },
    plugins: getPlugins()
  }
];
