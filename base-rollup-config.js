import fable from 'rollup-plugin-fable';

const { FABLE_SERVER_PORT: port = 61225 } = process.env;

export default {
  banner: '#!/usr/bin/env node',
  plugins: [
    fable({
      babel: {
        presets: [['env', { targets: { node: 'current' }, modules: false }]],
        plugins: [],
        babelrc: false
      },
      port
    })
  ]
};
