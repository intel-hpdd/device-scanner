// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

const normalizeVariablePaths = (paths, replacements) => {
  const newPaths = paths.reduce((o, path) => {
    const [, normalizedPath] = replacements.find(([regex]) =>
      regex.test(path)
    ) || ['', path];

    return [...o, normalizedPath];
  }, []);

  return newPaths;
};

const transformDevPath = (key, obj) => {
  obj.devpath = key;
  return obj;
};

const transformPaths = (replacements, obj) => {
  obj.paths = normalizeVariablePaths(obj.paths, replacements);
  return obj;
};

const transformDMUUID = (uuid, obj) => {
  obj.dmUuid = uuid;
  return obj;
};

const transformItem = (keyRegex, actualKey, transforms, data) => {
  return Object.keys(data).reduce((acc, key) => {
    const obj = Object.assign({}, data[key]);
    if (keyRegex.test(key))
      acc = Object.assign({}, acc, {
        [actualKey]: transforms.reduce((o, fn) => fn(key, o), obj)
      });
    else acc[key] = obj;

    return acc;
  }, {});
};

const transformEntries = (transforms, data) => {
  return transforms.reduce(
    (acc, [keyRegex, actualKey, propTransforms]) =>
      transformItem(keyRegex, actualKey, propTransforms, acc),
    data
  );
};

const sort = obj =>
  Object.keys(obj)
    .sort()
    .reduce((o, key) => {
      o[key] = obj[key];
      return o;
    }, {});

module.exports = {
  print(x, serialize) {
    const data = JSON.parse(x);

    const newData = transformEntries(
      [
        [
          /.+\/block\/sda$/,
          '/block/sda',
          [(key, obj) => transformDevPath('/block/sda', obj)]
        ],
        [
          /.+\/block\/sda\/sda1$/,
          '/block/sda/sda1',
          [
            (key, obj) => transformDevPath('/block/sda/sda1', obj),
            (key, obj) =>
              transformPaths(
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
          /.+\/block\/sda\/sda2$/,
          '/block/sda/sda2',
          [
            (key, obj) => transformDevPath('/block/sda/sda2', obj),
            (key, obj) =>
              transformPaths(
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
          /.+\/block\/sdb$/,
          '/block/sdb',
          [(key, obj) => transformDevPath('/block/sdb', obj)]
        ],
        [
          /.+\/block\/sdc$/,
          '/block/sdc',
          [(key, obj) => transformDevPath('/block/sdc', obj)]
        ],
        [
          /.+\/block\/sdd$/,
          '/block/sdd',
          [(key, obj) => transformDevPath('/block/sdd', obj)]
        ],
        [
          /.+\/block\/sde$/,
          '/block/sde',
          [(key, obj) => transformDevPath('/block/sde', obj)]
        ],
        [
          /.+\/block\/sdf$/,
          '/block/sdf',
          [(key, obj) => transformDevPath('/block/sdf', obj)]
        ],
        [
          /.+\/block\/sdg$/,
          '/block/sdg',
          [(key, obj) => transformDevPath('/block/sdg', obj)]
        ],
        [
          /.+\/block\/sdh$/,
          '/block/sdh',
          [(key, obj) => transformDevPath('/block/sdh', obj)]
        ],
        [
          /.+\/block\/sdi$/,
          '/block/sdi',
          [(key, obj) => transformDevPath('/block/sdi', obj)]
        ],
        [
          /.+\/block\/sdj$/,
          '/block/sdj',
          [(key, obj) => transformDevPath('/block/sdj', obj)]
        ],
        [
          /.+\/block\/sdk$/,
          '/block/sdk',
          [(key, obj) => transformDevPath('/block/sdk', obj)]
        ],
        [
          /.+\/block\/sdl$/,
          '/block/sdl',
          [(key, obj) => transformDevPath('/block/sdl', obj)]
        ],
        [
          /.+\/block\/sdm$/,
          '/block/sdm',
          [(key, obj) => transformDevPath('/block/sdm', obj)]
        ],
        [
          /.+\/block\/sdn$/,
          '/block/sdn',
          [(key, obj) => transformDevPath('/block/sdn', obj)]
        ],
        [
          /.+\/block\/sdo$/,
          '/block/sdo',
          [(key, obj) => transformDevPath('/block/sdo', obj)]
        ],
        [
          /.+\/block\/sdp$/,
          '/block/sdp',
          [(key, obj) => transformDevPath('/block/sdp', obj)]
        ],
        [
          /.+\/block\/sdq$/,
          '/block/sdq',
          [(key, obj) => transformDevPath('/block/sdq', obj)]
        ],
        [
          /.+\/block\/sdr$/,
          '/block/sdr',
          [(key, obj) => transformDevPath('/block/sdr', obj)]
        ],
        [
          /.+\/block\/sds$/,
          '/block/sds',
          [(key, obj) => transformDevPath('/block/sds', obj)]
        ],
        [
          /.+\/block\/sdt$/,
          '/block/sdt',
          [(key, obj) => transformDevPath('/block/sdt', obj)]
        ],
        [
          /.+\/block\/sdu$/,
          '/block/sdu',
          [(key, obj) => transformDevPath('/block/sdu', obj)]
        ],
        [
          /.+\/block\/sdv$/,
          '/block/sdv',
          [(key, obj) => transformDevPath('/block/sdv', obj)]
        ],
        [
          /.+\/block\/sdw$/,
          '/block/sdw',
          [(key, obj) => transformDevPath('/block/sdw', obj)]
        ],
        [
          /.+\/block\/sdx$/,
          '/block/sdx',
          [(key, obj) => transformDevPath('/block/sdx', obj)]
        ],
        [
          /.+\/block\/sdy$/,
          '/block/sdy',
          [(key, obj) => transformDevPath('/block/sdy', obj)]
        ],
        [
          /.+\/block\/sdz$/,
          '/block/sdz',
          [(key, obj) => transformDevPath('/block/sdz', obj)]
        ],
        [
          /.+\/block\/sdaa$/,
          '/block/sdaa',
          [(key, obj) => transformDevPath('/block/sdaa', obj)]
        ],
        [
          /.+\/block\/sdab$/,
          '/block/sdab',
          [(key, obj) => transformDevPath('/block/sdab', obj)]
        ],
        [
          /.+\/block\/sdac$/,
          '/block/sdac',
          [(key, obj) => transformDevPath('/block/sdac', obj)]
        ],
        [
          /.+\/block\/sdad$/,
          '/block/sdad',
          [(key, obj) => transformDevPath('/block/sdad', obj)]
        ],
        [
          /\/devices\/virtual\/block\/dm-0$/,
          '/devices/virtual/block/dm-0',
          [
            (key, obj) =>
              transformPaths(
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
              ),
            (key, obj) =>
              transformDMUUID(
                'LVM-FpAffE3HiAwoAvd81g8dBirIbkC3Ogu58AIfMI4SXo1AodrQkxuO2yuvd2JOPi5j',
                obj
              )
          ]
        ],
        [
          /\/devices\/virtual\/block\/dm-1$/,
          '/devices/virtual/block/dm-1',
          [
            (key, obj) =>
              transformPaths(
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
              ),
            (key, obj) =>
              transformDMUUID(
                'LVM-FpAffE3HiAwoAvd81g8dBirIbkC3Ogu50snmtLIcILKydXKz4EgqhgxR5Tf013zE',
                obj
              )
          ]
        ]
      ],
      data
    );

    try {
      return JSON.stringify(sort(newData), null, 2);
    } catch (e) {
      return serialize(x);
    }
  },

  test(x) {
    return x && typeof x === 'string';
  }
};
