// delaying wallaby automatic start
wallaby.delayStart();

requirejs.config({
  baseUrl: '/dist'
});

require(wallaby.tests, function () {
  wallaby.start();
});