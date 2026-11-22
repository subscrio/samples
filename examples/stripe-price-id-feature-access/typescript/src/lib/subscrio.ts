import { Subscrio } from 'subscrio';

let instance: Subscrio | undefined;

export function getSubscrio() {
  const connectionString = process.env.DATABASE_URL;
  if (!connectionString) throw new Error('Set DATABASE_URL to a dedicated PostgreSQL demo database.');
  return instance ??= new Subscrio({ database: { connectionString } });
}
