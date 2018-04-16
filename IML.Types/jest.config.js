module.exports = {
  preset: 'jest-fable-preprocessor',
  displayName: 'IML.Types tests',
  snapshotSerializers: ['../buffer-serializer'],
  coveragePathIgnorePatterns: [
    '.+fixtures.js',
    '.+Fixtures.fs',
    '/node_modules/',
    '.+\\.snap'
  ],
  modulePaths: ['<rootDir>/../node_modules']
};
