import { jsonDateParser } from 'json-date-parser';

export const baseUri = (process.env.NODE_ENV === 'production')
    ? "" : "http://localhost:6500";

export async function api(path: string) {
    const response = await fetch(`${baseUri}/api/${path}`);
    const json = await response.text();
    return JSON.parse(json, jsonDateParser);
}
