module.exports = {
  testEnvironment: 'node',
  moduleFileExtensions: ['js', 'fs', 'fsx'],
  transform: {
    '^.+\\.(fs|fsx)$': 'jest-fable-preprocessor',
    '^.+\\.js$': 'babel-jest'
  },
  testMatch: ['**/**/*.(test.fs|test.fsx)'],
  transformIgnorePatterns: ['node_modules/(?!fable.+)/'],
  coveragePathIgnorePatterns: ['packages', 'test/']
};
