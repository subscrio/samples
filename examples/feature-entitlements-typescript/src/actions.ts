import type { Subscrio } from 'subscrio';

export type ScheduleRequest = { episodeKey: string; publishAt: string };

// Arrays hold the work accepted by each entitlement check.
export class PodcastActions {
  readonly scheduled: Array<ScheduleRequest & { customerKey: string }> = [];
  readonly shows: Array<{ customerKey: string; title: string }> = [];
  readonly editorial: Array<{ customerKey: string; subject: string; queue: string }> = [];

  constructor(private readonly subscrio: Subscrio) {}

  async scheduleEpisode(customerKey: string, request: ScheduleRequest) {
    const allowed = await this.subscrio.featureChecker.isEnabledForCustomer(
      customerKey, 'castcoop', 'scheduled-publishing'
    );
    if (!allowed) return { error: 'scheduled_publishing_not_included' } as const;

    this.scheduled.push({ customerKey: customerKey, ...request });
    return { status: 'queued', ...request } as const;
  }

  async createShow(customerKey: string, title: string) {
    const limit = await this.subscrio.featureChecker.getValueForCustomer(
      customerKey, 'castcoop', 'show-limit', 0
    ) ?? 0;
    const used = this.shows.filter(show => show.customerKey === customerKey).length;
    if (used >= limit) return { error: 'show_limit_reached', limit, used } as const;

    this.shows.push({ customerKey: customerKey, title });
    return { status: 'created', title, limit, used: used + 1 } as const;
  }

  async requestEditorialHelp(customerKey: string, subject: string) {
    const route = await this.subscrio.featureChecker.getValueForCustomer(
      customerKey, 'castcoop', 'editorial-route', 'none'
    );
    if (route !== 'standard' && route !== 'priority') {
      return { error: 'editorial_support_not_included' } as const;
    }

    this.editorial.push({ customerKey: customerKey, subject, queue: route });
    return { status: 'queued', queue: route, subject } as const;
  }
}

