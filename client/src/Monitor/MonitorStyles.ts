import { style } from "typestyle";
import { NestedCSSProperties } from 'typestyle/lib/types';

export const monitorClass = style({
  backgroundColor: "#EEE",
  position: "absolute",
  right: "0",
  top: "0",
  width: "50%",
  bottom: "0"  
});

export const positionAnimation: NestedCSSProperties = {
  position: "absolute",
  transitionDuration: "300ms",
  transitionProperty: "left top width height right bottom"  
};

export const columnHeaderClass = style(positionAnimation, {
  fontStyle: "italic"
});

export const nodeClass = style(positionAnimation, {
  backgroundColor: "white",
  border: "1px solid silver",
  borderRadius: "8px",
  padding: "5px",  
});

export const nodeLabelClass = style({
  fontWeight: "bold"
});

export const nodeStatusItemClass = style({
  fontStyle: "italic",
  transitionDuration: "2s",
  transitionProperty: "background-color"
});

