import * as React from 'react';
import { PostShape, TimelineItemProps } from './PostShape';
import { LikedPost } from './TimelineModel';

export const LikedPostShape = ({post, owner}: TimelineItemProps<LikedPost>) => (
  <PostShape 
    post={post}
    owner={owner}
    action={(
      <>
        liked a post by&nbsp;
        <a href={`#${post.postedby[0].id}`}>
          {post.postedby[0].firstName} {post.postedby[0].lastName}
        </a>
      </>
    )}
  />
);

