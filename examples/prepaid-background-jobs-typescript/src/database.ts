import { Client } from 'pg';
import { randomUUID } from 'node:crypto';
export async function isolatedDatabase() {
  const connection = process.env.DATABASE_URL;
  if (!connection)
    throw new Error('Set DATABASE_URL to your local PostgreSQL development connection.');
  const admin = new Client({ connectionString: connection });
  await admin.connect();
  const name = 'subscrio_blog_' + randomUUID().replaceAll('-', '');
  try {
    await admin.query('CREATE DATABASE "' + name + '"');
  } catch (error) {
    await admin.end();
    throw error;
  }
  const url = new URL(connection);
  url.pathname = '/' + name;
  return {
    connectionString: url.toString(),
    async close() {
      try {
        if (!/^subscrio_blog_[a-f0-9]{32}$/.test(name))
          throw new Error('Unexpected database name');
        await admin.query('DROP DATABASE "' + name + '"');
      } finally {
        await admin.end();
      }
    },
  };
}
