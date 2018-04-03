module.exports = {
  preset: 'jest-fable-preprocessor',
  displayName: 'IML.Types tests',
  snapshotSerializers: ['../buffer-serializer'],
  coveragePathIgnorePatterns: [
    'fixtures',
    'Fixtures',
    '/node_modules/',
    '.+\\.snap'
  ]
};
