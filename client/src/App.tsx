import { observer } from 'mobx-react';
import * as moment from "moment";
import "moment/locale/en-gb";
import * as React from 'react';
import './App.css';
import { isLikedPost, isOwnerPost, TimelineModel } from './TimelineModel';

const model = new TimelineModel();

moment.locale("en-gb");

@observer
class App extends React.Component {
  public render() {
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
      <div>
        <h1>{owner.firstName} {owner.lastName}</h1>

        <div className="stats">
          Network: {network} ms | Logic: {logic} ms | Fetch: {fetch} ms
        </div>

        <div className="timeline">
        {
          model.unifiedTimeline.map(post => (
            isLikedPost(post) ? (
              <>
                <div className="timeline-item">
                  <span className="timeline-time">{moment(post.linked).format("L LT")}</span>
                  <span className="timeline-user">{owner.firstName}</span> liked a post by <a href={`#${post.postedby[0].id}`}>{post.postedby[0].firstName} {post.postedby[0].lastName}</a>
                </div>
                <div className="timeline-text">{post.text}</div>
              </>
            ) 
            :
            isOwnerPost(post) ? (
              <>
                <div className="timeline-item">
                  <span className="timeline-time">{moment(post.linked).format("L LT")}</span>
                  <span className="timeline-user">{owner.firstName}</span> posted an update:
                </div>
                <div className="timeline-text">{post.text}</div>
                {
                  post.likedby && post.likedby.length ? (
                    <div className="timeline-liked">
                        Liked by:
                        {
                          post.likedby.map(liker => (
                            <a href={`#${liker.id}`}>{liker.firstName} {liker.lastName}</a>
                          ))
                        }
                    </div>
                  ) 
                  :
                  undefined
                }                
              </>
            ) 
            :
            undefined
          ))
        }
        </div>        
      </div>
    );
  }
}

export default App;
