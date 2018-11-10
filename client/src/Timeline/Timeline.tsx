import { observer } from 'mobx-react';
import * as React from 'react';
import { TimelineItem } from './TimelineItem';
import { TimelineModel } from './TimelineModel';
import { statsClass, timelineClass } from './TimelineStyles';

export interface TimelineProps {
  model: TimelineModel;
}

export const Timeline = observer(({model}: TimelineProps) => {

  const timeline = model.timeline
  if (!timeline) {
    return <div/>;
  }

  const owner = timeline.entities[0];
  const stats = timeline.stats;

  const fetch = stats.timeFetching;
  const logic = stats.timeRunning - fetch;
  const network = timeline.overallTime - stats.timeRunning;

  return (
    <div className="timeline">
      <h1>{owner.firstName} {owner.lastName}</h1>

      <div className={statsClass}>
        Network: {network} ms | Logic: {logic} ms | Fetch: {fetch} ms
      </div>

      <div className={timelineClass}>
      {
        model.unifiedTimeline.map(post => (
          <TimelineItem key={post.id} post={post} owner={owner.firstName} />          
        ))
      }
      </div>        
    </div>
  );
});

