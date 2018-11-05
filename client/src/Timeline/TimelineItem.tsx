import * as React from 'react';
import { LikedPostShape } from './LikedPostShape';
import { OwnerPostShape } from './OwnerPostShape';
import { TimelineItemProps } from './PostShape';
import { isLikedPost, isOwnerPost, Post } from './TimelineModel';

export const TimelineItem = ({post, owner}: TimelineItemProps<Post>) => (
  isLikedPost(post) ? (
    <LikedPostShape post={post} owner={owner}/>
  )
  :
  isOwnerPost(post) ? (
    <OwnerPostShape post={post} owner={owner}/>
  ) 
  :
  <div/>
);

