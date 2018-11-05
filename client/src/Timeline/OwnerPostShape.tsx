import * as React from 'react';
import { PostShape, TimelineItemProps } from './PostShape';
import { OwnerPost } from './TimelineModel';
import { timelineLikedClass } from './TimelineStyles';

export const OwnerPostShape = ({post, owner}: TimelineItemProps<OwnerPost>) => (
  <PostShape
    post={post}
    owner={owner}
    action="posted an update"
    content={(
      post.likedby && post.likedby.length ? (
        <div className={timelineLikedClass}>
            Liked by:
            {
              post.likedby.map(liker => (
                <a key={liker.id} href={`#${liker.id}`}>{liker.firstName} {liker.lastName}</a>
              ))
            }
        </div>
      ) 
      :
      undefined
    )}    
  />
);
