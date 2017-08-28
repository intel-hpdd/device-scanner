import fable from 'rollup-plugin-fable';
import cleanup from 'rollup-plugin-cleanup';
import filesize from 'rollup-plugin-filesize';

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
    }),
    cleanup(),
    filesize()
  ]
};
