module.exports = {
  preset: 'jest-fable-preprocessor',
  displayName: 'DeviceScannerDaemon tests',
  snapshotSerializers: ['../buffer-serializer'],
  modulePaths: ['<rootDir>/../node_modules']
};
