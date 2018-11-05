import { observer } from 'mobx-react';
import * as moment from "moment";
import "moment/locale/en-gb";
import * as React from 'react';
import { Monitor } from './Monitor/Monitor';
import { MonitorModel } from './Monitor/MonitorModel';
import { Timeline } from "./Timeline/Timeline";
import { TimelineModel } from './Timeline/TimelineModel';

const timeline = new TimelineModel();
const monitor = new MonitorModel();

moment.locale("en-gb");

@observer
class App extends React.Component {
  public render() {
    
    return (
      <div className="root">
        <Timeline model={timeline} />
        <Monitor model={monitor} />        
      </div>
    );
  }
}

export default App;
