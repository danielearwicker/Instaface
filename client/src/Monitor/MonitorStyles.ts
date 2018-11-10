import { style } from "typestyle";
import { NestedCSSProperties } from 'typestyle/lib/types';

export const positionAnimation: NestedCSSProperties = {
  position: "absolute",
  transitionDuration: "300ms",
  transitionProperty: "left top width height right bottom"  
};

export const columnHeaderClass = style(positionAnimation, {
  fontStyle: "italic",
  fontSize: "0.7em"
});

export const nodeClass = style(positionAnimation, {
  border: "1px solid #ddd",
  borderRadius: "4px",
  boxShadow: "4px 4px 4px #eee",
  padding: "5px",  
  fontSize: "0.7em",
  $nest: {
    "label *": {
      verticalAlign: "middle"
    }
  }
});

export const nodeLabelClass = style({
  fontWeight: "bold",
  whiteSpace: "nowrap",
  overflow: "hidden"
});

export const nodeStatusItemClass = style({
  fontStyle: "italic",
  borderRadius: "4px",
  transitionDuration: "2s",
  transitionProperty: "background-color"
});

