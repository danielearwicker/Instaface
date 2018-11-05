export interface SourcedEvent {    
    source: string;
}

export interface LeaderEvent extends SourcedEvent {
    type: "leader"
}

export interface FollowerEvent extends SourcedEvent {
    type: "follower"
    leader: string;
}

export interface ReachableEvent extends SourcedEvent {
    type: "reachable"
    about: string;
    term: number;
    reachable: boolean;
}

export interface CandidacyEvent extends SourcedEvent {
    type: "candidacy"
    enabled: boolean;
    term: number;
}

export interface CacheEvent extends SourcedEvent {
    type: "cache"
    hit: boolean;
    ids: number[];
    kind: "entity" | "association";
}

export type NodeEvent = LeaderEvent | FollowerEvent |
    ReachableEvent | CandidacyEvent | CacheEvent;