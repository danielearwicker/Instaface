import { observable } from 'mobx';
import { MonitorModel } from './MonitorModel';
import { StatusItem } from './StatusItem';

export class NodeModel {

    @observable
    public left = 0;

    @observable
    public top = 0;

    @observable
    public width = 0;

    @observable
    public statusItems: StatusItem[] = [];

    @observable
    public leader: boolean | undefined;

    @observable
    public unplugged = false;

    public readonly label: string;

    @observable
    private reachableValue = true;
    
    private term = -1;

    get reachable() {
        return this.reachableValue;
    }

    constructor(private readonly owner: MonitorModel, 
                public readonly key: string) {
        this.label = extractLabel(key);
    }

    public status(key: string): StatusItem {
        return getKeyedItem(this.statusItems, key, k => new StatusItem(k));        
    }

    public setRole(term: number, leader: boolean) {
        if (term >= this.term) {
            this.leader = leader;
            this.term = term;

            if (leader) {
                this.reachableValue = true;
            }
        }
    }

    public setReachable(term: number, value: boolean) {
        if (term >= this.term) {
            this.reachableValue = value;
            this.term = term;

            if (!value) {
                this.leader = false;
            }
        }
    }

    public setUnplugged = async (e: React.ChangeEvent<HTMLInputElement>) => {
        e.stopPropagation();
        e.preventDefault();

        const unplugged = !e.target.checked;
        await this.owner.setUnplugged(this.key, unplugged);
        this.unplugged = unplugged;
    }
}

export function getKeyedItem<T extends { readonly key: string }>(
    items: T[],
    key: string,
    type: (key: string) => T
) {
    let item = items.find(s => s.key === key);
    if (item) {
        return item;
    }

    item = type(key);
    items.push(item);
    return item;
}

function extractLabel(key: string) {
    let m = /([\d\.]+)/.exec(key);
    if (m) { return m[1] };

    m = /localhost:([\d]+)/.exec(key);
    if (m) { return m[1] };

    return "?";
}
