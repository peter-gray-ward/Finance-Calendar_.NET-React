import React, { useMemo } from 'react';
import './App.scss';
import { ModalItem } from './types';

export default function Modal({ 
    items,
    className,
    origin,
    originPosition
}: { 
    items: ModalItem[],
    className?: string,
    origin: React.RefObject<HTMLButtonElement|null>,
    originPosition: { [key: string]: number }
}) {
    const originHeight = origin.current!.getBoundingClientRect().height;
    return <ul style={{left: originPosition.left + 'px', top: originPosition.top + originHeight + 'px'}} className={`Modal${className ? ' ' + className : ''}`}>
        {
            items.map((item: ModalItem, index: number) => (
                <li key={index}>
                    <span>{item.label}</span>
                </li>
            ))
        }
    </ul>
}