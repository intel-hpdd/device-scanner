import baseConfig from '../../base-rollup-config.js';

export default Object.assign({}, baseConfig, {
  input: 'IML/BlockDeviceListener/Listener.fs',
  external: ['net'],
  output: {
    file: './dist/block-device-listener/block-device-listener',
    format: 'cjs'
  }
});
