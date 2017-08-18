# line-delimited-json-stream

A Transform stream that buffers JSON objects. It will emit a completed object on a newline.

Useful for sending messages over a bi-directional stream (socket) between server and clients.

## Overview

Messages will be buffered by this stream until a full JSON object has been received terminated by a newline.

Consider if someone emits a partial object (or one comes over the stream):

```
                     ┌─────────────────────────────┐
                     │        {"foo": "bar"        │
┌───────────────┐    └─────────────────────────────┘     ┌───────────────┐
│    Client     │ ────────────────────────────────────▶  │    Server     │
└───────────────┘                                        └───────────────┘
```

At this point, the stream is holding a partial object and will not emit.
Now consider what happens when the rest is sent:

```
                     ┌─────────────────────────────┐
                     │       , "bar": "baz"}\n     │
┌───────────────┐    └─────────────────────────────┘     ┌───────────────┐
│    Client     │ ────────────────────────────────────▶  │    Server     │
└───────────────┘                                        └───────────────┘
```

Now we have received a full JSON object terminated by a newline.
The stream will emit the completed and parsed object:

`{"foo": "bar", "bar": "baz"}`
