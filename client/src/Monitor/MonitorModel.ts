import delay from 'delay';
import { action, observable } from 'mobx';
import { api } from 'src/api';
import { ColumnModel } from './ColumnModel';
import { NodeEvent } from './Events';
import { registerMonitor } from './MonitoringConnection';
import { getKeyedItem, NodeModel } from "./NodeModel";

export class MonitorModel {

    @observable
    public nodes: NodeModel[] = [];

    @observable
    public columns: ColumnModel[] = [
        new ColumnModel("Followers", 0),
        new ColumnModel("Leader", 100),
        new ColumnModel("Unreachable", 200),
    ];

    private widthElement: HTMLElement | null = null;

    constructor() {
        registerMonitor(this);
        this.trackWidth();
    }
    
    public setWidthElement = (widthElement: HTMLElement | null) => {
        this.widthElement = widthElement;
        this.updatePositions();
    }

    public node(key: string): NodeModel {
        return getKeyedItem(this.nodes, key, k => new NodeModel(this, k));        
    }

    public async connected() {
        const state: ClusterState = await api("timeline/cluster");

        this.node(state.leader).setRole(-1, true);
        state.followers.forEach(f => this.node(f).setRole(-1, false));
        state.unreachable.forEach(f => this.node(f).setReachable(-1, false));
        
        this.updatePositions();
    }

    public disconnected() { this.nodes.length = 0; }

    public event(info: NodeEvent) {
        // tslint:disable-next-line:no-console
        console.log(info);

        switch (info.type) {
            case "leader":
                this.node(info.source).setRole(info.term, true);
                break;
            case "follower":
                this.node(info.source).setRole(info.term, false);
                break;
            case "reachable":
                this.node(info.about).setReachable(info.term, info.reachable);
                break;
            case "candidacy":
                this.node(info.source).status("candidacy").text = 
                    info.enabled ? `Standing: ${info.term}` 
                                 : `Abandoned: ${info.term}`;                                 
                break;

            case "cache":
                const outcome = info.hit ? "Hit" : "Miss";
                this.node(info.source).status(`cache${outcome}`).text =
                    `${outcome}: ${info.kind} x ${info.ids.length}`;
                break;
        }

        this.updatePositions();
    }

    @action
    public updatePositions() {        
        const gap = 24;
        
        const width = this.widthElement ? this.widthElement.clientWidth : 400;

        const available = width - (gap * (this.columns.length + 2));
        const columnWidth = available / this.columns.length;

        for (let c = 0; c < this.columns.length; c++) {
            const column = this.columns[c];
            column.left = gap + c * (columnWidth + gap);
            column.top = gap; 
            column.width = columnWidth;
        }

        const lineDepth = 14;
        
        const updateColumn = (column: number, predicate: (n: NodeModel) => boolean) => {
            let y = gap + lineDepth * 1.5;
  
            for (const node of this.nodes.filter(predicate)) {
                node.left = this.columns[column].left;
                node.top = y;
                node.width = columnWidth;
                y += gap + (lineDepth * (1 + node.statusItems.length));
            }
        }

        updateColumn(0, n => n.reachable && !n.leader);
        updateColumn(1, n => !!n.leader);
        updateColumn(2, n => !n.reachable && n.leader === false);
    }
    
    public async setUnplugged(node: string, unplugged: boolean) {
        for (const n of this.nodes) {
            await fetch(`${n.key}/api/graph/unplugged`, {
                method: 'PUT',
                body: JSON.stringify({ node, unplugged }),
                headers:{
                  'Content-Type': 'application/json'
                }
            });
        }
    }

    private async trackWidth() {
        for (;;) {                        
            await delay(1000);
            this.updatePositions();
        }
    }
}

interface ClusterState {
    leader: string;
    followers: string[];
    unreachable: string[];
    unplugged: string[];
}
