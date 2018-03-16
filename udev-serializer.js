/*eslint no-undef: "error"*/
/*eslint-env node*/

const buffer = require('buffer');

const normalizeVariablePaths = (o, replacements) => {
  return {
    Array: o.Array.map(({ String }) => {
      const matcher = replacements.find(([matcher]) => {
        return matcher.test(String);
      });

      if (matcher) {
        const [, replacement] = matcher;
        return { String: replacement };
      } else {
        return { String };
      }
    })
  };
};

const transformEntry = (key, fn, data) => {
  return {
    [key]: {
      Object: data.Object.map(([k, v]) => {
        return fn(k, v);
      })
    }
  };
};

const transformPaths = (key, replacements, obj) =>
  transformEntry(
    key,
    (k, v) => {
      if (k === 'PATHS') return [k, normalizeVariablePaths(v, replacements)];
      else return [k, v];
    },
    obj
  );

const transformDevPath = (key, obj) =>
  transformEntry(
    key,
    (k, v) => {
      if (k === 'DEVPATH') return [k, { String: key }];
      else return [k, v];
    },
    obj
  );

const transformDMUUID = (key, uuid, obj) =>
  transformEntry(
    key,
    (k, v) => {
      if (k === 'DM_UUID') return [k, { String: uuid }];
      else return [k, v];
    },
    obj
  );

const transformItem = (keyRegex, transforms, data) => {
  return Object.keys(data).reduce((acc, key) => {
    const obj = Object.assign({}, data[key]);
    if (keyRegex.test(key))
      acc = Object.assign(
        {},
        acc,
        transforms.reduce((o, fn) => fn(key, o), obj)
      );
    else acc[key] = obj;

    return acc;
  }, {});
};

const transformEntries = (transforms, data) =>
  transforms.reduce(
    (acc, [keyRegex, propTransforms]) =>
      transformItem(keyRegex, propTransforms, acc),
    data
  );

module.exports = {
  print(x, serialize) {
    const val = x.toString();
    const data = JSON.parse(val);

    const newData = transformEntries(
      [
        [
          /.+\/block\/sda\/sda1/,
          [
            (key, obj) =>
              transformPaths(
                key,
                [
                  [
                    /\/dev\/disk\/by-uuid/,
                    '/dev/disk/by-uuid/74b3fabd-dbf5-4cc0-a967-2c12f8113fa6'
                  ]
                ],
                obj
              )
          ]
        ],
        [
          /.+\/block\/sda\/sda2/,
          [
            (key, obj) =>
              transformPaths(
                key,
                [
                  [
                    /\/dev\/disk\/by-id\/lvm-pv-uuid/,
                    '/dev/disk/by-id/lvm-pv-uuid-tVAIF5-nJY7-oKaP-wLQO-OIXp-Ggwn-0F9F70'
                  ]
                ],
                obj
              )
          ]
        ],
        [
          /\/devices\/virtual\/block\/dm-0/,
          [
            (key, obj) =>
              transformPaths(
                key,
                [
                  [
                    /\/dev\/disk\/by-id\/dm-uuid-LVM/,
                    '/dev/disk/by-id/dm-uuid-LVM-FpAffE3HiAwoAvd81g8dBirIbkC3Ogu58AIfMI4SXo1AodrQkxuO2yuvd2JOPi5j'
                  ],
                  [
                    /\/dev\/disk\/by-uuid\//,
                    '/dev/disk/by-uuid/45252d52-d8d6-468e-aaa0-c117b042944a'
                  ]
                ],
                obj
              )
          ]
        ],
        [
          /\/devices\/virtual\/block\/dm-1/,
          [
            (key, obj) =>
              transformPaths(
                key,
                [
                  [
                    /\/dev\/disk\/by-id\/dm-uuid-LVM/,
                    '/dev/disk/by-id/dm-uuid-LVM-FpAffE3HiAwoAvd81g8dBirIbkC3Ogu50snmtLIcILKydXKz4EgqhgxR5Tf013zE'
                  ],
                  [
                    /\/dev\/disk\/by-uuid\//,
                    '/dev/disk/by-uuid/4af3b3ab-356b-490b-9aab-2e9786804b79'
                  ]
                ],
                obj
              )
          ]
        ],
        [
          /\/devices\/virtual\/block\/dm-0/,
          [
            (key, obj) =>
              transformDMUUID(
                key,
                'LVM-FpAffE3HiAwoAvd81g8dBirIbkC3Ogu58AIfMI4SXo1AodrQkxuO2yuvd2JOPi5j',
                obj
              )
          ]
        ],
        [
          /\/devices\/virtual\/block\/dm-1/,
          [
            (key, obj) =>
              transformDMUUID(
                key,
                'LVM-FpAffE3HiAwoAvd81g8dBirIbkC3Ogu50snmtLIcILKydXKz4EgqhgxR5Tf013zE',
                obj
              )
          ]
        ],
        [
          /\/devices\/platform\/host\d+\/session\d+\/target\d+\:\d+\:\d+\/\d+\:\d+\:\d+\:\d+\/block\/sdae/,
          [
            (key, obj) =>
              transformDevPath(
                '/devices/platform/host38/session7/target38:0:0/38:0:0:0/block/sdae',
                obj
              )
          ]
        ],
        [
          /\/devices\/platform\/host\d+\/session\d+\/target\d+\:\d+\:\d+\/\d+\:\d+\:\d+\:\d+\/block\/sdaf/,
          [
            (key, obj) =>
              transformDevPath(
                '/devices/platform/host38/session7/target38:0:0/38:0:0:1/block/sdaf',
                obj
              )
          ]
        ],
        [
          /\/devices\/platform\/host\d+\/session\d+\/target\d+\:\d+\:\d+\/\d+\:\d+\:\d+\:\d+\/block\/sdag/,
          [
            (key, obj) =>
              transformDevPath(
                '/devices/platform/host39/session8/target39:0:0/39:0:0:0/block/sdag',
                obj
              )
          ]
        ],
        [
          /\/devices\/platform\/host\d+\/session\d+\/target\d+\:\d+\:\d+\/\d+\:\d+\:\d+\:\d+\/block\/sdah/,
          [
            (key, obj) =>
              transformDevPath(
                '/devices/platform/host39/session8/target39:0:0/39:0:0:1/block/sdah',
                obj
              )
          ]
        ]
      ],
      data
    );

    try {
      return serialize(JSON.stringify(newData, null, 2));
    } catch (e) {
      return serialize(val);
    }
  },

  test(x) {
    return x && buffer.Buffer.isBuffer(x);
  }
};
