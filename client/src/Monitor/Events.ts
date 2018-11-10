export interface SourcedEvent {    
    source: string;
}

export interface TermedEvent extends SourcedEvent {
    term: number;
}

export interface LeaderEvent extends TermedEvent {
    type: "leader"
}

export interface FollowerEvent extends TermedEvent {
    type: "follower"
    leader: string;
}

export interface ReachableEvent extends TermedEvent {
    type: "reachable"
    about: string;    
    reachable: boolean;
}

export interface CandidacyEvent extends TermedEvent {
    type: "candidacy"
    enabled: boolean;
}

export interface CacheEvent extends SourcedEvent {
    type: "cache"
    hit: boolean;
    ids: number[];
    kind: "entity" | "association";
}

export type NodeEvent = LeaderEvent | FollowerEvent |
    ReachableEvent | CandidacyEvent | CacheEvent;