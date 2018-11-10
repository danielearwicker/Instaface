import { observer } from 'mobx-react';
import * as React from 'react';
import { MonitorModel } from './MonitorModel';
import { columnHeaderClass, nodeClass, nodeLabelClass, nodeStatusItemClass } from './MonitorStyles';
import { StatusItem } from './StatusItem';

export interface MonitorProps {
    model: MonitorModel;
}

function getPositionStyle(item: { left: number; top: number; width: number; }) {
    return { left: `${item.left}px`, top: `${item.top}px`, width: `${item.width}px` };
}

export const Monitor = observer(({model}: MonitorProps) => {

    return (
        <div className="monitor" ref={model.setWidthElement}>
        {
            model.columns.map(column => (
                <div className={columnHeaderClass} style={getPositionStyle(column)}>{column.label}</div>
            ))
        }
        {
            model.nodes.map(node => (
                <div key={node.key} className={nodeClass} style={getPositionStyle(node)}>
                    <div className={nodeLabelClass}>
                    <label>
                        <input type="checkbox" checked={!node.unplugged} onChange={node.setUnplugged} />
                        {node.label}
                    </label>
                    </div>
                    {
                        node.statusItems.map(status => <StatusLine key={status.key} item={status}/>)
                    }
                </div>
            ))
        }
        </div>
    );
});

interface StatusLineProps {
    item: StatusItem;
}

const StatusLine = observer(({item}: StatusLineProps) => {

    const opacity = Math.max(0, 1 - (item.age / 3000));

    const color = item.key === "candidacy" ? "255, 255, 180" :
                  item.key === "cacheHit" ? "90, 255, 90" :
                  item.key === "cacheMiss" ? "255, 90, 90" :
                  "180, 255, 255";

    const style = {
        backgroundColor: `rgba(${color}, ${opacity})`
    }

    return (
        <div className={nodeStatusItemClass} style={style}>
            {item.text}
        </div>
    );
});