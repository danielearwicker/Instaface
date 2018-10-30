import { jsonDateParser } from "json-date-parser";
import { computed, observable } from "mobx";

export interface Entity {
    id: number;
    linked: Date;
    type: string;
    created: Date;
}

export interface User extends Entity {
    lastName: string;
    firstName: string;
}

export interface Post extends Entity {
    text: string;
}

export interface OwnerPost extends Post {
    likedby: User[];
}

export interface LikedPost extends Post {
    postedby: User[];
}

export function isOwnerPost(post: Post): post is OwnerPost {
    return "likedby" in post;
}

export function isLikedPost(post: Post): post is LikedPost {
    return "postedby" in post;
}

export interface TimelineOwner extends User {
    posted: OwnerPost[];
    liked: LikedPost[];
}

export interface Timeline {
    stats: {
        timeRunning: number;
        calls: number;
        timeFetching: number;
    };
    entities: TimelineOwner[];
    overallTime: number;
}

export class TimelineModel {

    @observable
    public timeline: Timeline | undefined;

    @computed
    get unifiedTimeline() {

        if (!this.timeline) {
            return [];
        }

        const owner = this.timeline.entities[0];
        const posted: Post[] = owner.posted;
        const liked: Post[] = owner.liked;
        const parts = posted.concat(liked);
        parts.sort((a, b) => b.linked.getTime() - a.linked.getTime());

        return parts;
    }

    constructor() {
        window.addEventListener("hashchange", () => this.load());

        this.load();
    }

    private async load() {
        const id = parseInt(location.hash.substr(1), 10);
        if (isNaN(id)) {
            this.timeline = undefined;
            return;
        }
        
        const response = await fetch(`http://localhost:6543/api/timeline/${id}`);
        const json = await response.text();        
        this.timeline = JSON.parse(json, jsonDateParser);
    }
}
