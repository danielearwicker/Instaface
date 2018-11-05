import { style } from "typestyle";

export const timelineWrapperClass = style({
  position: "absolute",
  left: "0",
  top: "0",
  width: "50%",
  bottom: "0",
  overflow: "auto"
});

export const statsClass = style({
  color: "silver",
  fontSize: "0.6em"
});

export const timelineClass = style({
  padding: "0 2em"
});

export const timelineItemClass = style({
  marginTop: "2em"
});

export const timelineUserClass = style({
  fontWeight: "bold"
});

export const timelineTimeClass = style({
  color: "silver",
  paddingRight: "1em"
});

export const timelineTextClass = style({
  fontStyle: "italic",
  margin: "1em 2em",
});

export const timelineLikedClass = style({
  $nest: {
    a: {
      paddingLeft: "0.5em"
    }
  },
  marginLeft: "2em"
});
