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
    input: 'IML.DeviceScannerDaemon/src/IML.DeviceScannerDaemon.fsproj',
    external: ['stream', 'net', 'child_process', 'buffer', '@iml/node-libzfs'],
    output: {
      file: './dist/device-scanner-daemon/device-scanner-daemon',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.ScannerProxyDaemon/src/IML.ScannerProxyDaemon.fsproj',
    external: [
      'stream',
      'net',
      'child_process',
      'buffer',
      'https',
      'fs',
      'path',
      'url'
    ],
    output: {
      file: './dist/scanner-proxy-daemon/scanner-proxy-daemon',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.DeviceAggregatorDaemon/src/IML.DeviceAggregatorDaemon.fsproj',
    external: ['stream', 'child_process', 'buffer', 'http'],
    output: {
      file: './dist/device-aggregator-daemon/device-aggregator-daemon',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.Listeners/UdevListener/UdevListener.fsproj',
    external: ['net', 'buffer'],
    output: {
      banner: '#!/usr/bin/env node',
      file: './dist/listeners/udev-listener',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.Listeners/MountEmitter/src/IML.MountEmitter.fsproj',
    external: ['net', 'buffer', 'stream'],
    output: {
      banner: '#!/usr/bin/env node',
      file: './dist/listeners/mount-emitter',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.Listeners/HistoryEventZedlet/HistoryEventZedlet.fsproj',
    external: ['net', 'buffer'],
    output: {
      banner: '#!/usr/bin/env node',
      file: './dist/listeners/history_event-scanner.sh',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.Listeners/PoolCreateZedlet/PoolCreateZedlet.fsproj',
    external: ['net', 'buffer'],
    output: {
      banner: '#!/usr/bin/env node',
      file: './dist/listeners/pool_create-scanner.sh',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.Listeners/PoolDestroyZedlet/PoolDestroyZedlet.fsproj',
    external: ['net', 'buffer'],
    output: {
      banner: '#!/usr/bin/env node',
      file: './dist/listeners/pool_destroy-scanner.sh',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.Listeners/PoolExportZedlet/PoolExportZedlet.fsproj',
    external: ['net', 'buffer'],
    output: {
      banner: '#!/usr/bin/env node',
      file: './dist/listeners/pool_export-scanner.sh',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.Listeners/PoolImportZedlet/PoolImportZedlet.fsproj',
    external: ['net', 'buffer'],
    output: {
      banner: '#!/usr/bin/env node',
      file: './dist/listeners/pool_import-scanner.sh',
      format: 'cjs'
    },
    plugins: getPlugins()
  },
  {
    input: 'IML.Listeners/VdevAddZedlet/VdevAddZedlet.fsproj',
    external: ['net', 'buffer'],
    output: {
      banner: '#!/usr/bin/env node',
      file: './dist/listeners/vdev_add-scanner.sh',
      format: 'cjs'
    },
    plugins: getPlugins()
  }
];
