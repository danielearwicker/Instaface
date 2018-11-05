import * as signalR from "@aspnet/signalr";
import delay from "delay";
import { baseUri } from '../api';

export interface MonitoringEvents {
    connected(): void;
    disconnected(): void;
    event(info: {}): void;
};

const reconnectAfter = 5000;

const monitors: MonitoringEvents[] = [];

async function start() {
    for (;;) {
        const connection = new signalR.HubConnectionBuilder().withUrl(`${baseUri}/monitoring`).build();

        connection.onclose(async () => {
            monitors.forEach(m => m.disconnected());
            await delay(reconnectAfter);
            await start();
        });

        connection.on("event", info => monitors.forEach(m => m.event(info)));

        try {
            await connection.start();
            monitors.forEach(m => m.connected());
            return;
        } catch (x) {
            await delay(reconnectAfter);
        }
    }
}

export function registerMonitor(m: MonitoringEvents) {
    monitors.push(m);
}

start();
