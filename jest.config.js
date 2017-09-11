module.exports = {
  verbose: true,
  expand: true,
  resetModules: true,
  clearMocks: true,
  testEnvironment: 'node',
  moduleFileExtensions: ['js', 'fs'],
  transform: {
    '^.+\\.(fs)$': 'jest-fable-preprocessor',
    '^.+\\.js$': 'jest-fable-preprocessor/source/babel-jest.js'
  },
  testMatch: ['**/**/*Test.fs'],
  coveragePathIgnorePatterns: ['packages', '.+Test.fs']
};
