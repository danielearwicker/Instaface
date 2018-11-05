import * as moment from "moment";
import * as React from 'react';
import { Post } from './TimelineModel';
import { timelineItemClass, timelineTextClass, 
         timelineTimeClass, timelineUserClass } from './TimelineStyles';

export interface TimelineItemProps<P extends Post> {
  post: P;
  owner: string;
}
  
export interface PostShapeProps extends TimelineItemProps<Post> {
  action: React.ReactNode;
  content?: React.ReactNode;
}

export const PostShape = ({post, action, content, owner}: PostShapeProps) => (
  <>
    <div className={timelineItemClass}>
      <span className={timelineTimeClass}>{moment(post.linked).format("L LT")}</span>
      <span className={timelineUserClass}>{owner}</span> {action}:
    </div>
    <div className={timelineTextClass}>{post.text}</div>
    {content}
  </>
);
