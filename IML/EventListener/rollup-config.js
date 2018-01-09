import baseConfig from '../../base-rollup-config.js';

export default Object.assign({}, baseConfig, {
  input: 'IML/EventListener/Listener.fs',
  external: ['net', 'buffer', 'stream'],
  output: {
    banner: '#!/usr/bin/env node',
    file: './dist/event-listener/event-listener',
    format: 'cjs'
  }
});
