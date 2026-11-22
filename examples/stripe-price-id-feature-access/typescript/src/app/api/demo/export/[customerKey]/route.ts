import { getSubscrio } from '@/lib/subscrio';

export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';

export async function GET(_request: Request, context: { params: Promise<{ customerKey: string }> }) {
  const { customerKey } = await context.params;
  if (!['legacy-team', 'current-team', 'free-team'].includes(customerKey))
    return Response.json({ error: 'demo_customer_not_found' }, { status: 404 });

  const allowed = await getSubscrio().featureChecker.isEnabledForCustomer(
    customerKey,
    'feedbackdesk',
    'csv-export',
  );

  if (!allowed) return Response.json({ error: 'export_not_included' }, { status: 403 });
  return new Response('feedback,votes\nDark mode,12', {
    headers: { 'content-type': 'text/csv; charset=utf-8' },
  });
}
