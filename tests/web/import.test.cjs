const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../../web/collection-card/app.js'), 'utf8');
// Load the actual pure function declarations and shared data, without executing
// DOM event bindings or initiating network requests/profile publication.
const constants = source.slice(0, source.indexOf('const elements ='));
const declarations = source.slice(source.indexOf('function readFile('));
const context = vm.createContext({ console, Intl, URL, currentUserIdentifiers: {}, ownedCardMapCache: new WeakMap() });
vm.runInContext(constants + '\n' + declarations + '\n globalThis.importApi = {buildProfile, extractRawUserIdentifiers};', context);
const raw = fs.readFileSync(path.join(__dirname, '../fixtures/collection-v3.json'), 'utf8');
const fixture = JSON.parse(raw);
const lookup = { loaded: false, byId: new Map(), byDbf: new Map(), bySet: new Map(), all: [], count: 0 };
test('schema3 import retains permanent/premium counts and dust', () => {
  const profile = context.importApi.buildProfile(fixture, 'fixture.json', lookup);
  assert.equal(profile.ownedCards, 5);
  assert.equal(profile.goldenCards, 1);
  assert.equal(profile.availableDust, 123);
  assert.equal(profile.setBreakdown[0].unique, 1);
  assert.equal(profile.coverageText, '100% в импортированных данных');
});

test('completion uses the full catalog, excludes special sets and ignores duplicate rows', () => {
  const card = fixture.cards[0];
  const all = [{ id: card.cardId, dbfId: card.dbfId, set: card.set }, { id: 'MISSING', dbfId: 999, set: card.set }, { id: 'CORE', set: 'CORE' }, { id: 'EVENT', set: 'EVENT' }];
  const catalog = { ...lookup, loaded: true, all, byId: new Map(all.map(c => [c.id, c])), byDbf: new Map(), bySet: new Map() };
  const profile = context.importApi.buildProfile({ ...fixture, cards: [card, card] }, 'fixture.json', catalog);
  assert.equal(profile.coverageRatio, 0.5);
  assert.equal(profile.coverageText, '50% коллекционных карт каталога');
});
test('account IDs beyond JS integer precision are extracted exactly', () => {
  const user = context.importApi.extractRawUserIdentifiers(raw);
  assert.equal(String(user.accountHi), '18446744073709551615');
  assert.equal(String(user.accountLo), '9007199254740993');
});
test('changes and empty collections are rejected', () => {
  assert.throws(() => context.importApi.buildProfile({ ...fixture, exportType: 'changes' }, 'delta.json', lookup), /полный JSON/);
  assert.throws(() => context.importApi.buildProfile({ ...fixture, cards: [] }, 'empty.json', lookup), /не найдены карты/);
});
