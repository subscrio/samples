import type { Subscrio } from 'subscrio';

export type Actor = { customerKey: string; permissions: readonly string[] };
export type ScheduleRequest = { episodeKey: string; publishAt: string };

// These fixtures stand in for the application's own storage and authenticated context.
export class PodcastActions {
  readonly scheduled: Array<ScheduleRequest & { customerKey: string }> = [];
  readonly shows: Array<{ customerKey: string; title: string }> = [];
  readonly editorial: Array<{ customerKey: string; subject: string; queue: string }> = [];

  constructor(private readonly subscrio: Subscrio) {}

  async scheduleEpisode(actor: Actor, request: ScheduleRequest) {
    if (!actor.permissions.includes('publish')) return { error: 'permission_denied' } as const;
    const allowed = await this.subscrio.featureChecker.isEnabledForCustomer(
      actor.customerKey, 'castcoop', 'scheduled-publishing'
    );
    if (!allowed) return { error: 'scheduled_publishing_not_included' } as const;

    this.scheduled.push({ customerKey: actor.customerKey, ...request });
    return { status: 'queued', ...request } as const;
  }

  async createShow(actor: Actor, title: string) {
    if (!actor.permissions.includes('manage-shows')) return { error: 'permission_denied' } as const;
    const limit = await this.subscrio.featureChecker.getValueForCustomer(
      actor.customerKey, 'castcoop', 'show-limit', 0
    ) ?? 0;
    const used = this.shows.filter(show => show.customerKey === actor.customerKey).length;
    if (used >= limit) return { error: 'show_limit_reached', limit, used } as const;

    this.shows.push({ customerKey: actor.customerKey, title });
    return { status: 'created', title, limit, used: used + 1 } as const;
  }

  async requestEditorialHelp(actor: Actor, subject: string) {
    if (!actor.permissions.includes('request-help')) return { error: 'permission_denied' } as const;
    const route = await this.subscrio.featureChecker.getValueForCustomer(
      actor.customerKey, 'castcoop', 'editorial-route', 'none'
    );
    if (route !== 'standard' && route !== 'priority') {
      return { error: 'editorial_support_not_included' } as const;
    }

    this.editorial.push({ customerKey: actor.customerKey, subject, queue: route });
    return { status: 'queued', queue: route, subject } as const;
  }
}
