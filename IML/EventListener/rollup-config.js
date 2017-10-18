import baseConfig from '../../base-rollup-config.js';

export default Object.assign({}, baseConfig, {
  input: 'IML/EventListener/Listener.fs',
  external: ['net'],
  output: {
    file: './dist/event-listener/event-listener',
    format: 'cjs'
  }
});
