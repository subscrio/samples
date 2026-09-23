import {createServer} from 'node:http';
import {once} from 'node:events';
import assert from 'node:assert/strict';
export async function replayPublicationRequests(publish: (key: string) => Promise<{status: number; body: object}>) {
  const server = createServer(async (req,res) => {
    const key = req.url === '/customers/local-club/public-bracket' ? 'local-club' : req.url === '/customers/regional-open/public-bracket' ? 'regional-open' : null;
    if (req.method !== 'POST' || !key) {res.writeHead(404).end();return;}
    try { const result = await publish(key);res.writeHead(result.status, {'content-type':'application/json'}).end(JSON.stringify(result.body)); }
    catch {res.writeHead(500).end();}
  });
  server.listen(0,'127.0.0.1'); await once(server,'listening');
  const address = server.address(); if (!address || typeof address === 'string') throw new Error('Missing test port');
  try {
    for (const [key,status,body] of [['local-club',403,{error:'live_brackets_not_included'}],['regional-open',201,{status:'published'}]] as const) {
      const response: Response = await fetch('http://127.0.0.1:'+address.port+'/customers/'+key+'/public-bracket',{method:'POST'});
      const actual = await response.json(); assert.equal(response.status,status);assert.deepEqual(actual,body);
      console.log(key + ': HTTP ' + response.status + ' ' + JSON.stringify(actual));
    }
  } finally {server.closeAllConnections(); await new Promise<void>((resolve,reject)=>server.close(error=>error?reject(error):resolve()));}
}
